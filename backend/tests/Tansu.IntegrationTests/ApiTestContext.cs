using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;
using Tansu.Infrastructure.Persistence;
using Tansu.Infrastructure.Seeding;

namespace Tansu.IntegrationTests;

public sealed class ApiTestContext(ApiFactory factory)
{
    public const string VerifyServiceKey = "test-verify-service-key-32chars-min!!";
    public const string EmployeeTestPassword = IntegrationTestAuth.EmployeeTestPassword;

    private readonly HttpClient _http = factory.CreateClient();
    private SeededIds? _ids;
    private string? _adminToken;
    private string? _subcontractorToken;
    private string? _employeeToken;

    public HttpClient Http => _http;

    public async Task<SeededIds> GetIdsAsync()
    {
        if (_ids is not null)
            return _ids;

        await using var scope = factory.Services.CreateAsyncScope();
        var ctx = scope.ServiceProvider.GetRequiredService<TansuDbContext>();

        var projectOid = await ctx.ProjectRefs.Select(p => p.ProjectOid).FirstAsync();
        var subcontractorId = await ctx.Subcontractors.Select(s => s.Id).FirstAsync();
        var employeeId = await ctx.Employees.Select(e => e.Id).FirstAsync();
        var userId = await ctx.Users.Where(u => u.UserType == UserType.Tansu).Select(u => u.Id).FirstAsync();
        var delegateUserId = await ctx.Users
            .Where(u => u.UserType == UserType.Tansu && u.IsActive && u.Id != userId)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (delegateUserId == Guid.Empty)
            delegateUserId = userId;
        var batchId = await ctx.EmployeeApprovalBatches.Select(b => b.Id).FirstOrDefaultAsync();
        if (batchId == Guid.Empty)
            batchId = Guid.NewGuid();

        var approvalSheetId = await ctx.ApprovalSheet.Select(s => s.Id).FirstOrDefaultAsync();
        if (approvalSheetId == Guid.Empty)
            approvalSheetId = Guid.NewGuid();

        var documentRequestId = await ctx.DocumentRequests.Select(r => r.Id).FirstOrDefaultAsync();
        if (documentRequestId == Guid.Empty)
            documentRequestId = Guid.NewGuid();

        var documentRequestSheetId = await ctx.DocumentApprovalSheet.Select(s => s.Id).FirstOrDefaultAsync();
        if (documentRequestSheetId == Guid.Empty)
            documentRequestSheetId = Guid.NewGuid();

        var employeeDocumentId = await ctx.EmployeeDocuments.Select(d => d.Id).FirstOrDefaultAsync();
        if (employeeDocumentId == Guid.Empty)
            employeeDocumentId = Guid.NewGuid();

        var projectDocumentId = await ctx.ProjectDocuments.Select(d => d.Id).FirstOrDefaultAsync();
        if (projectDocumentId == Guid.Empty)
            projectDocumentId = Guid.NewGuid();

        var subcontractorDocumentId = await ctx.SubcontractorDocuments.Select(d => d.Id).FirstOrDefaultAsync();
        if (subcontractorDocumentId == Guid.Empty)
            subcontractorDocumentId = Guid.NewGuid();

        var ppeIssuanceId = await ctx.EmployeePpeIssuances.Select(p => p.Id).FirstOrDefaultAsync();
        if (ppeIssuanceId == Guid.Empty)
            ppeIssuanceId = Guid.NewGuid();

        var accessPassToken = await ctx.EmployeeAccessPasses
            .Where(p => p.RevokedAt == null)
            .Select(p => p.Token)
            .FirstOrDefaultAsync() ?? "missing-access-pass-token";

        var employeeIin = await ctx.Employees
            .Where(e => e.Id == employeeId)
            .Select(e => e.Iin)
            .FirstAsync();

        _ids = new SeededIds(
            projectOid,
            subcontractorId,
            employeeId,
            userId,
            delegateUserId,
            batchId,
            approvalSheetId,
            documentRequestId,
            documentRequestSheetId,
            employeeDocumentId,
            projectDocumentId,
            subcontractorDocumentId,
            ppeIssuanceId,
            accessPassToken,
            employeeIin,
            Guid.NewGuid());

        return _ids;
    }

    public async Task<string?> GetTokenAsync(ApiAuthKind auth)
    {
        return auth switch
        {
            ApiAuthKind.Anonymous or ApiAuthKind.VerifyServiceKey => null,
            ApiAuthKind.TansuOnly or ApiAuthKind.Authenticated => await GetAdminTokenAsync(),
            ApiAuthKind.SubcontractorOnly => await GetSubcontractorTokenAsync(),
            ApiAuthKind.EmployeeOnly => await GetEmployeeTokenAsync(),
            _ => null
        };
    }

    public string ResolvePath(string template, SeededIds ids) =>
        template
            .Replace("{projectOid}", ids.ProjectOid.ToString())
            .Replace("{subcontractorId}", ids.SubcontractorId.ToString())
            .Replace("{employeeId}", ids.EmployeeId.ToString())
            .Replace("{userId}", ids.UserId.ToString())
            .Replace("{batchId}", ids.BatchId.ToString())
            .Replace("{sheetId}", ids.ApprovalSheetId.ToString())
            .Replace("{documentRequestId}", ids.DocumentRequestId.ToString())
            .Replace("{documentId}", ids.EmployeeDocumentId.ToString())
            .Replace("{subcontractorDocumentId}", ids.SubcontractorDocumentId.ToString())
            .Replace("{projectDocumentId}", ids.ProjectDocumentId.ToString())
            .Replace("{documentRequestSheetId}", ids.DocumentRequestSheetId.ToString())
            .Replace("{issuanceId}", ids.PpeIssuanceId.ToString())
            .Replace("{accessPassToken}", ids.AccessPassToken)
            .Replace("{missingId}", ids.MissingId.ToString());

    public string ResolveQuery(string template, SeededIds ids) =>
        template
            .Replace("{projectOid}", ids.ProjectOid.ToString())
            .Replace("{subcontractorId}", ids.SubcontractorId.ToString());

    public async Task<HttpResponseMessage> SendAsync(ApiEndpoint endpoint)
    {
        var ids = await GetIdsAsync();
        var path = ResolvePath(endpoint.PathTemplate, ids);
        if (!string.IsNullOrEmpty(endpoint.Query))
            path += "?" + ResolveQuery(endpoint.Query, ids);

        var request = new HttpRequestMessage(endpoint.Method, path)
        {
            Headers = { ConnectionClose = true }
        };
        await ApplyAuthAsync(request, endpoint.Auth);
        ApplyBody(request, endpoint, ids);

        return await _http.SendAsync(request);
    }

    private async Task ApplyAuthAsync(HttpRequestMessage request, ApiAuthKind auth)
    {
        if (auth == ApiAuthKind.VerifyServiceKey)
        {
            request.Headers.Add("X-Tansu-Verify-Key", VerifyServiceKey);
            return;
        }

        var token = await GetTokenAsync(auth);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ApplyBody(HttpRequestMessage request, ApiEndpoint endpoint, SeededIds ids)
    {
        switch (endpoint.Body)
        {
            case ApiRequestBody.JsonEmpty:
            case ApiRequestBody.JsonMinimal:
                request.Content = JsonContent.Create(BuildRequestBody(endpoint.Id, ids));
                break;
            case ApiRequestBody.FormEmpty:
                request.Content = new MultipartFormDataContent();
                break;
        }
    }

    private static object BuildRequestBody(string endpointId, SeededIds ids) => endpointId switch
    {
        "auth.login" => new { email = DemoSeedData.SubMontazhEmail, password = "wrong-password" },
        "auth.dev-login" => new { email = DemoSeeder.TansuAdminEmail },
        "auth.me-project-progress" => new { completionPercent = 10 },
        "employee-portal.login" => new { iin = "000000000000", password = "wrong" },
        "employees.issue-ppe" => new { itemType = "helmet" },
        "photo-reviews.reject" => new { reason = "test" },
        "employee-portal.submit-quiz" => new
        {
            answers = new Dictionary<string, string>
            {
                ["q1"] = "a",
                ["q2"] = "b",
                ["q3"] = "b",
                ["q4"] = "b",
                ["q5"] = "a"
            }
        },
        "matrix.set" => new { steps = Array.Empty<object>() },
        "document-matrix.set" => new { steps = Array.Empty<object>() },
        "delegations.create" => new
        {
            delegateUserId = ids.DelegateUserId,
            validFrom = DateTimeOffset.UtcNow.AddDays(-1),
            validTo = DateTimeOffset.UtcNow.AddDays(7)
        },
        "incidents.create" => new
        {
            projectOid = ids.ProjectOid,
            occurredAt = DateTimeOffset.UtcNow,
            title = "Тестовый инцидент",
            description = "Проверка API",
            severity = "low",
            blockUntilResolved = false,
            employeeIds = Array.Empty<Guid>()
        },
        "incidents.update" => new { status = "resolved", resolutionNotes = "закрыт" },
        _ => new { }
    };

    private async Task<string> GetAdminTokenAsync()
    {
        if (_adminToken is not null)
            return _adminToken;

        var res = await _http.PostAsJsonAsync("/api/auth/dev-login", new { email = DemoSeeder.TansuAdminEmail });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<LoginPayload>();
        _adminToken = body!.AccessToken;
        return _adminToken;
    }

    private async Task<string> GetSubcontractorTokenAsync()
    {
        if (_subcontractorToken is not null)
            return _subcontractorToken;

        var res = await _http.PostAsJsonAsync("/api/auth/login", new
        {
            email = DemoSeedData.SubMontazhEmail,
            password = DemoSeedData.SubcontractorTempPassword
        });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<LoginPayload>();
        _subcontractorToken = body!.AccessToken;
        return _subcontractorToken;
    }

    private async Task<string> GetEmployeeTokenAsync()
    {
        if (_employeeToken is not null)
            return _employeeToken;

        _employeeToken = await IntegrationTestAuth.GetEmployeeAccessTokenAsync(factory);
        return _employeeToken;
    }

    private sealed record LoginPayload(string AccessToken);

    public sealed record SeededIds(
        Guid ProjectOid,
        Guid SubcontractorId,
        Guid EmployeeId,
        Guid UserId,
        Guid DelegateUserId,
        Guid BatchId,
        Guid ApprovalSheetId,
        Guid DocumentRequestId,
        Guid DocumentRequestSheetId,
        Guid EmployeeDocumentId,
        Guid ProjectDocumentId,
        Guid SubcontractorDocumentId,
        Guid PpeIssuanceId,
        string AccessPassToken,
        string EmployeeIin,
        Guid MissingId);
}
