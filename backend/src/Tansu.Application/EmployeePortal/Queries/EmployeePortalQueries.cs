using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tansu.Application.AccessPasses.Commands;
using Tansu.Application.AccessPasses;
using Tansu.Application.AccessPasses.Queries;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeePortal.Queries;

public sealed record GetEmployeePortalDashboardQuery : IRequest<EmployeePortalDashboardDto>;

public sealed class GetEmployeePortalDashboardHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IOptions<AccessPassOptions> accessPassOptions)
    : IRequestHandler<GetEmployeePortalDashboardQuery, EmployeePortalDashboardDto>
{
    public async Task<EmployeePortalDashboardDto> Handle(GetEmployeePortalDashboardQuery req, CancellationToken ct)
    {
        var employee = await LoadCurrentEmployeeAsync(db, currentUser, ct);
        var sheets = await db.ApprovalSheet.AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id)
            .ToListAsync(ct);
        var approvalStatus = EmployeeStatusResolver.ResolveFromSheets(sheets);
        var isApproved = approvalStatus == ApprovalStatus.Approved;

        var quiz = await db.EmployeeSafetyQuizCompletions.AsNoTracking()
            .FirstOrDefaultAsync(q => q.EmployeeId == employee.Id, ct);

        EmployeePortalPassDto? passDto = null;
        var pass = await db.EmployeeAccessPasses.AsNoTracking()
            .Where(p => p.EmployeeId == employee.Id && p.RevokedAt == null)
            .OrderByDescending(p => p.IssuedAt)
            .FirstOrDefaultAsync(ct);

        var canShowQr = isApproved && quiz is not null && pass is not null;
        if (canShowQr && pass is not null)
        {
            passDto = new EmployeePortalPassDto(
                pass.Id,
                IssueEmployeeAccessPassHandler.BuildVerifyUrl(
                    accessPassOptions.Value.VerifyWebBaseUrl,
                    pass.Token),
                pass.IssuedAt,
                !string.IsNullOrEmpty(employee.PhotoPath));
        }

        var activePpe = await db.EmployeePpeIssuances.AsNoTracking()
            .Where(p => p.EmployeeId == employee.Id && p.ReturnedAt == null)
            .Select(p => p.ItemType)
            .ToListAsync(ct);
        var hasHelmet = activePpe.Contains(PpeItemType.Helmet);
        var hasUniform = activePpe.Contains(PpeItemType.Uniform);

        return new EmployeePortalDashboardDto(
            employee.Id,
            employee.FullName,
            employee.Position,
            EmployeePortalTexts.BuildWorkDescription(employee.Position, employee.Project?.Name),
            employee.Subcontractor?.Name ?? "—",
            employee.Project?.Name,
            approvalStatus,
            isApproved,
            quiz is not null,
            quiz?.Score,
            quiz?.TotalQuestions,
            canShowQr,
            passDto,
            hasHelmet,
            hasUniform);
    }

    internal static async Task<Domain.Entities.Employee> LoadCurrentEmployeeAsync(
        ITansuDbContext db,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Employee || currentUser.EmployeeId is null)
            throw new ForbiddenException();

        return await db.Employees.AsNoTracking()
            .Include(e => e.Subcontractor)
            .Include(e => e.Project)
            .FirstOrDefaultAsync(e => e.Id == currentUser.EmployeeId, ct)
            ?? throw new NotFoundException("Employee", currentUser.EmployeeId.Value);
    }
}

public sealed record GetSafetyQuizQuery : IRequest<IReadOnlyList<SafetyQuizQuestionDto>>;

public sealed class GetSafetyQuizHandler(ICurrentUser currentUser)
    : IRequestHandler<GetSafetyQuizQuery, IReadOnlyList<SafetyQuizQuestionDto>>
{
    public Task<IReadOnlyList<SafetyQuizQuestionDto>> Handle(GetSafetyQuizQuery req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Employee)
            throw new ForbiddenException();

        return Task.FromResult(SafetyQuizCatalog.Questions);
    }
}

public sealed record SubmitSafetyQuizCommand(IReadOnlyDictionary<string, string> Answers)
    : IRequest<SafetyQuizSubmitResult>;

public sealed class SubmitSafetyQuizHandler(
    ITansuDbContext db,
    ICurrentUser currentUser) : IRequestHandler<SubmitSafetyQuizCommand, SafetyQuizSubmitResult>
{
    public async Task<SafetyQuizSubmitResult> Handle(SubmitSafetyQuizCommand req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);

        var existing = await db.EmployeeSafetyQuizCompletions
            .FirstOrDefaultAsync(q => q.EmployeeId == employee.Id, ct);
        if (existing is not null)
        {
            return new SafetyQuizSubmitResult(
                true,
                existing.Score,
                existing.TotalQuestions,
                "Опрос по ТБ уже пройден.");
        }

        var (score, total, passed) = SafetyQuizCatalog.Grade(req.Answers);
        if (!passed)
        {
            return new SafetyQuizSubmitResult(
                false,
                score,
                total,
                $"Нужно ответить правильно на все вопросы ({score}/{total}). Попробуйте снова.");
        }

        db.EmployeeSafetyQuizCompletions.Add(new Domain.Entities.EmployeeSafetyQuizCompletion
        {
            EmployeeId = employee.Id,
            Score = score,
            TotalQuestions = total
        });
        await db.SaveChangesAsync(ct);

        return new SafetyQuizSubmitResult(
            true,
            score,
            total,
            "Опрос пройден. QR-пропуск доступен в личном кабинете.");
    }
}

public sealed record GetEmployeePortalQrQuery : IRequest<byte[]?>;

public sealed class GetEmployeePortalQrHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IAccessPassQrEncoder qrEncoder,
    IOptions<AccessPassOptions> options) : IRequestHandler<GetEmployeePortalQrQuery, byte[]?>
{
    public async Task<byte[]?> Handle(GetEmployeePortalQrQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);

        var quizDone = await db.EmployeeSafetyQuizCompletions.AsNoTracking()
            .AnyAsync(q => q.EmployeeId == employee.Id, ct);
        if (!quizDone)
            throw new ForbiddenException("Сначала пройдите опрос по технике безопасности.");

        var pass = await db.EmployeeAccessPasses.AsNoTracking()
            .Where(p => p.EmployeeId == employee.Id && p.RevokedAt == null)
            .OrderByDescending(p => p.IssuedAt)
            .FirstOrDefaultAsync(ct);
        if (pass is null)
            return null;

        var payload = IssueEmployeeAccessPassHandler.BuildVerifyUrl(
            options.Value.VerifyWebBaseUrl,
            pass.Token);
        return qrEncoder.EncodePng(payload);
    }
}

internal static class EmployeePortalTexts
{
    public static string BuildWorkDescription(string position, string? projectName)
    {
        var project = string.IsNullOrWhiteSpace(projectName) ? "объекте" : $"объекте «{projectName}»";
        return $"Вы допущены к работе в должности «{position}» на {project}. "
               + "Соблюдайте правила техники безопасности, используйте СИЗ и следуйте указаниям ответственных на площадке. "
               + "Для физического доступа на объект необходимо пройти опрос по ТБ и предъявить QR-пропуск с проверкой Face ID на проходной.";
    }
}
