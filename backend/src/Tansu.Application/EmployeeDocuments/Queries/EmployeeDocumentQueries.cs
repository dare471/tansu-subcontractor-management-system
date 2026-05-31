using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeeDocuments.Commands;
using Tansu.Application.EmployeePortal.Queries;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeeDocuments.Queries;

public sealed record GetEmployeeDocumentsQuery(Guid EmployeeId) : IRequest<EmployeeDocumentsSummaryDto>;

public sealed class GetEmployeeDocumentsHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<GetEmployeeDocumentsQuery, EmployeeDocumentsSummaryDto>
{
    public async Task<EmployeeDocumentsSummaryDto> Handle(GetEmployeeDocumentsQuery req, CancellationToken ct)
    {
        await EmployeeDocumentAuthorization.EnsureEmployeeAccessAsync(
            req.EmployeeId, currentUser, db, accessService, ct);

        var now = DateTimeOffset.UtcNow;
        var threshold = now.AddDays(14);

        var rows = await db.EmployeeDocuments.AsNoTracking()
            .Where(d => d.EmployeeId == req.EmployeeId)
            .Include(d => d.UploadedBy)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        var supersededIds = await UploadEmployeeDocumentHandler.LoadSupersededIdsAsync(db, req.EmployeeId, ct);
        var versionMap = ComputeVersionNumbers(rows);

        var dtos = rows.Select(d =>
        {
            var dto = EmployeeDocumentMapper.ToDto(d, now, supersededIds);
            return dto with { VersionNo = versionMap.GetValueOrDefault(d.Id, 1) };
        }).ToList();

        var expiring = dtos.Count(d => d.IsExpiringSoon);

        return new EmployeeDocumentsSummaryDto(dtos, dtos.Count, expiring);
    }

    internal static Dictionary<Guid, int> ComputeVersionNumbers(
        IReadOnlyList<Domain.Entities.EmployeeDocument> rows)
    {
        var byId = rows.ToDictionary(r => r.Id);

        int Depth(Guid id)
        {
            if (!byId.TryGetValue(id, out var doc) || doc.SupersedesDocumentId is null)
                return 1;
            return 1 + Depth(doc.SupersedesDocumentId.Value);
        }

        return rows.ToDictionary(r => r.Id, r => Depth(r.Id));
    }
}

public sealed record GetEmployeeDocumentFileQuery(Guid EmployeeId, Guid DocumentId)
    : IRequest<(Stream Stream, string FileName, string ContentType)?>;

public sealed class GetEmployeeDocumentFileHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IEmployeeDocumentStorage storage) : IRequestHandler<GetEmployeeDocumentFileQuery, (Stream Stream, string FileName, string ContentType)?>
{
    public async Task<(Stream Stream, string FileName, string ContentType)?> Handle(
        GetEmployeeDocumentFileQuery req,
        CancellationToken ct)
    {
        await EmployeeDocumentAuthorization.EnsureEmployeeAccessAsync(
            req.EmployeeId, currentUser, db, accessService, ct);

        var doc = await db.EmployeeDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.EmployeeId == req.EmployeeId, ct);
        if (doc is null) return null;

        var stream = await storage.OpenReadAsync(doc.FilePath, ct);
        if (stream is null) return null;

        var ext = Path.GetExtension(doc.FilePath);
        var contentType = doc.ContentType ?? "application/octet-stream";
        return (stream, $"{doc.Name}{ext}", contentType);
    }
}

public sealed record GetEmployeeBlockStatusQuery(Guid EmployeeId) : IRequest<EmployeeBlockStatusDto>;

public sealed class GetEmployeeBlockStatusHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService) : IRequestHandler<GetEmployeeBlockStatusQuery, EmployeeBlockStatusDto>
{
    public async Task<EmployeeBlockStatusDto> Handle(GetEmployeeBlockStatusQuery req, CancellationToken ct)
    {
        await EmployeeDocumentAuthorization.EnsureEmployeeAccessAsync(
            req.EmployeeId, currentUser, db, accessService, ct);

        var rows = await db.EmployeeBlockRecords.AsNoTracking()
            .Where(r => r.EmployeeId == req.EmployeeId)
            .Include(r => r.InitiatedBy)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var history = rows.Select(EmployeeDocumentMapper.ToBlockDto).ToList();
        var last = history.FirstOrDefault();
        var isBlocked = last?.ActionType == EmployeeBlockActionType.Block;

        return new EmployeeBlockStatusDto(isBlocked, last, history);
    }
}

public sealed record GetEmployeePortalDocumentsQuery : IRequest<EmployeeDocumentsSummaryDto>;

public sealed class GetEmployeePortalDocumentsHandler(ITansuDbContext db, ICurrentUser currentUser, IMediator mediator)
    : IRequestHandler<GetEmployeePortalDocumentsQuery, EmployeeDocumentsSummaryDto>
{
    public async Task<EmployeeDocumentsSummaryDto> Handle(GetEmployeePortalDocumentsQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        return await mediator.Send(new GetEmployeeDocumentsQuery(employee.Id), ct);
    }
}

public sealed record GetEmployeePortalDocumentFileQuery(Guid DocumentId)
    : IRequest<(Stream Stream, string FileName, string ContentType)?>;

public sealed class GetEmployeePortalDocumentFileHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IMediator mediator)
    : IRequestHandler<GetEmployeePortalDocumentFileQuery, (Stream Stream, string FileName, string ContentType)?>
{
    public async Task<(Stream Stream, string FileName, string ContentType)?> Handle(
        GetEmployeePortalDocumentFileQuery req,
        CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        var file = await mediator.Send(new GetEmployeeDocumentFileQuery(employee.Id, req.DocumentId), ct);
        return file;
    }
}

public sealed record GetEmployeePortalBlockStatusQuery : IRequest<EmployeeBlockStatusDto>;

public sealed class GetEmployeePortalBlockStatusHandler(ITansuDbContext db, ICurrentUser currentUser, IMediator mediator)
    : IRequestHandler<GetEmployeePortalBlockStatusQuery, EmployeeBlockStatusDto>
{
    public async Task<EmployeeBlockStatusDto> Handle(GetEmployeePortalBlockStatusQuery req, CancellationToken ct)
    {
        var employee = await GetEmployeePortalDashboardHandler.LoadCurrentEmployeeAsync(db, currentUser, ct);
        return await mediator.Send(new GetEmployeeBlockStatusQuery(employee.Id), ct);
    }
}
