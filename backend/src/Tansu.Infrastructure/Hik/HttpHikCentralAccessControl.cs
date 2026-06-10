using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Infrastructure.Hik;

public sealed class HttpHikCentralAccessControl(
    IHttpClientFactory httpClientFactory,
    IOptions<HikCentralOptions> options,
    ILogger<HttpHikCentralAccessControl> logger) : IAccessControlSystem
{
    public const string HttpClientName = "hikcentral";

    private static readonly DateTimeOffset MaxValidTo =
        new(2037, 12, 30, 23, 59, 59, TimeSpan.Zero);

    private static readonly JsonSerializerOptions WriteJson = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions ReadJson = new(JsonSerializerDefaults.Web);

    public string VendorId => "hik";

    public async Task SyncPersonAsync(AccessControlPerson person, CancellationToken ct)
    {
        var opts = options.Value;
        var personCode = ResolvePersonCode(person);
        var (familyName, givenName) = SplitName(person.FullName);
        var (beginTime, endTime) = BuildValidity(person);
        var phone = Normalize(person.Phone);
        var cards = string.IsNullOrWhiteSpace(person.CardNumber)
            ? null
            : new[] { new CardItem(person.CardNumber!.Trim()) };
        var faces = person.PhotoBytes is { Length: > 0 }
            ? new[] { new FaceItem(Convert.ToBase64String(person.PhotoBytes)) }
            : null;

        var existingId = await FindPersonIdAsync(personCode, ct);

        string personId;
        if (existingId is null)
        {
            var body = new PersonWriteRequest(
                null, personCode, familyName, givenName, opts.OrgIndexCode,
                phone, beginTime, endTime, cards, faces);
            var data = await PostAsync<PersonAddResponse>(
                "/artemis/api/resource/v1/person/single/add", body, ct);
            personId = data?.PersonId
                ?? throw new InvalidOperationException("HikCentral add person: пустой personId в ответе.");
            logger.LogInformation(
                "HikCentral: создан сотрудник {EmployeeId} (personId {PersonId}, code {Code})",
                person.EmployeeId, personId, personCode);
        }
        else
        {
            personId = existingId;
            var body = new PersonWriteRequest(
                personId, personCode, familyName, givenName, opts.OrgIndexCode,
                phone, beginTime, endTime, cards, faces);
            await PostAsync<object>(
                "/artemis/api/resource/v1/person/single/update", body, ct);
            logger.LogInformation(
                "HikCentral: обновлён сотрудник {EmployeeId} (personId {PersonId})",
                person.EmployeeId, personId);
        }

        await UpdatePositionAsync(personId, person.Position, ct);
        await AddToAccessGroupsAsync(personId, ct);
    }

    public async Task RevokePersonAsync(Guid employeeId, string reason, CancellationToken ct)
    {
        var opts = options.Value;
        var personCode = employeeId.ToString("N");
        var personId = await FindPersonIdAsync(personCode, ct);
        if (personId is null)
        {
            logger.LogInformation(
                "HikCentral: сотрудник {EmployeeId} не найден, блокировка не требуется", employeeId);
            return;
        }

        var hasGroups = opts.AccessGroupIndexCodes is { Count: > 0 }
            && opts.AccessGroupIndexCodes.Any(g => !string.IsNullOrWhiteSpace(g));

        if (opts.BlockMode == HikCentralBlockMode.RemoveFromAccessGroups && hasGroups)
        {
            await RemoveFromAccessGroupsAsync(personId, ct);
            logger.LogInformation(
                "HikCentral: сотрудник {EmployeeId} снят с групп доступа (personId {PersonId}). Причина: {Reason}",
                employeeId, personId, reason);
            return;
        }

        await PostAsync<object>(
            "/artemis/api/resource/v1/person/single/delete",
            new PersonDeleteRequest(personId), ct);
        logger.LogInformation(
            "HikCentral: удалён сотрудник {EmployeeId} (personId {PersonId}). Причина: {Reason}",
            employeeId, personId, reason);
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        try
        {
            await PostAsync<object>(
                "/artemis/api/resource/v1/person/advance/personList",
                new PersonListRequest(1, 1, null), ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HikCentral health-check не прошёл");
            return false;
        }
    }

    private async Task<string?> FindPersonIdAsync(string personCode, CancellationToken ct)
    {
        var data = await PostAsync<PersonListResponse>(
            "/artemis/api/resource/v1/person/advance/personList",
            new PersonListRequest(1, 10, personCode), ct);

        return data?.List?
            .FirstOrDefault(p => string.Equals(p.PersonCode, personCode, StringComparison.Ordinal))?
            .PersonId;
    }

    private async Task UpdatePositionAsync(string personId, string? position, CancellationToken ct)
    {
        var fieldId = options.Value.PositionCustomFieldId;
        if (string.IsNullOrWhiteSpace(fieldId) || string.IsNullOrWhiteSpace(position))
            return;

        await PostAsync<object>(
            "/artemis/api/resource/v1/person/personId/customFieldsUpdate",
            new CustomFieldsUpdateRequest(
                personId,
                new[] { new CustomFieldItem(fieldId!, position!.Trim()) }), ct);
    }

    private async Task AddToAccessGroupsAsync(string personId, CancellationToken ct)
    {
        foreach (var groupId in EnumerateGroups())
        {
            await PostAsync<object>(
                "/artemis/api/acs/v1/privilege/group/single/addPersons",
                new PrivilegeGroupPersonsRequest(groupId, 1, new[] { new PrivilegeGroupPerson(personId) }), ct);
        }
    }

    private async Task RemoveFromAccessGroupsAsync(string personId, CancellationToken ct)
    {
        foreach (var groupId in EnumerateGroups())
        {
            await PostAsync<object>(
                "/artemis/api/acs/v1/privilege/group/single/deletePersons",
                new PrivilegeGroupPersonsRequest(groupId, 1, new[] { new PrivilegeGroupPerson(personId) }), ct);
        }
    }

    private IEnumerable<string> EnumerateGroups() =>
        (options.Value.AccessGroupIndexCodes ?? new List<string>())
        .Where(g => !string.IsNullOrWhiteSpace(g))
        .Select(g => g.Trim());

    private async Task<T?> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var opts = options.Value;
        var json = JsonSerializer.Serialize(body, WriteJson);
        using var request = HikCentralSigner.CreateSignedJsonPost(path, json, opts.AppKey, opts.AppSecret);

        var http = httpClientFactory.CreateClient(HttpClientName);
        using var response = await http.SendAsync(request, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"HikCentral {path} вернул HTTP {(int)response.StatusCode}: {Trim(raw)}");
        }

        var envelope = JsonSerializer.Deserialize<HikEnvelope<T>>(raw, ReadJson);
        if (envelope is null)
            throw new InvalidOperationException($"HikCentral {path}: пустой ответ.");

        if (!string.Equals(envelope.Code, "0", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"HikCentral {path} вернул code={envelope.Code} msg={envelope.Msg}");
        }

        return envelope.Data;
    }

    private string ResolvePersonCode(AccessControlPerson person) =>
        !string.IsNullOrWhiteSpace(person.PersonCode)
            ? person.PersonCode!.Trim()
            : person.EmployeeId.ToString("N");

    private static (string? BeginTime, string? EndTime) BuildValidity(AccessControlPerson person)
    {
        if (person.ValidFrom is null && person.ValidTo is null)
            return (null, null);

        var begin = person.ValidFrom ?? DateTimeOffset.UtcNow;
        var end = person.ValidTo ?? MaxValidTo;
        if (end > MaxValidTo)
            end = MaxValidTo;

        return (Iso(begin), Iso(end));
    }

    private static string Iso(DateTimeOffset value) => value.ToString("yyyy-MM-ddTHH:mm:sszzz");

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (string Family, string Given) SplitName(string fullName)
    {
        var trimmed = (fullName ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return ("-", "-");

        var idx = trimmed.IndexOf(' ');
        return idx <= 0
            ? (trimmed, trimmed)
            : (trimmed[..idx], trimmed[(idx + 1)..].Trim());
    }

    private static string Trim(string s) => s.Length > 500 ? s[..500] : s;

    private sealed record HikEnvelope<T>(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("msg")] string? Msg,
        [property: JsonPropertyName("data")] T? Data);

    private sealed record FaceItem(
        [property: JsonPropertyName("faceData")] string FaceData);

    private sealed record CardItem(
        [property: JsonPropertyName("cardNo")] string CardNo);

    private sealed record PersonWriteRequest(
        string? PersonId,
        string PersonCode,
        string PersonFamilyName,
        string PersonGivenName,
        string OrgIndexCode,
        string? PhoneNo,
        string? BeginTime,
        string? EndTime,
        IReadOnlyList<CardItem>? Cards,
        IReadOnlyList<FaceItem>? Faces);

    private sealed record PersonAddResponse(
        [property: JsonPropertyName("personId")] string? PersonId);

    private sealed record PersonDeleteRequest(string PersonId);

    private sealed record PersonListRequest(int PageNo, int PageSize, string? PersonCode);

    private sealed record PersonListResponse(
        [property: JsonPropertyName("list")] IReadOnlyList<PersonListItem>? List);

    private sealed record PersonListItem(
        [property: JsonPropertyName("personId")] string? PersonId,
        [property: JsonPropertyName("personCode")] string? PersonCode);

    private sealed record PrivilegeGroupPerson(
        [property: JsonPropertyName("id")] string Id);

    private sealed record PrivilegeGroupPersonsRequest(
        string PrivilegeGroupId,
        int Type,
        IReadOnlyList<PrivilegeGroupPerson> List);

    private sealed record CustomFieldItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("customFieldValue")] string CustomFieldValue);

    private sealed record CustomFieldsUpdateRequest(
        string PersonId,
        IReadOnlyList<CustomFieldItem> List);
}
