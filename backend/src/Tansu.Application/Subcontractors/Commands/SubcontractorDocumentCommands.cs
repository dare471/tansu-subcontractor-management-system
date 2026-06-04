using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record ListSubcontractorDocumentsQuery(Guid SubcontractorId)
    : IRequest<IReadOnlyList<SubcontractorDocumentDto>>;

public sealed class ListSubcontractorDocumentsHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<ListSubcontractorDocumentsQuery, IReadOnlyList<SubcontractorDocumentDto>>
{
    public async Task<IReadOnlyList<SubcontractorDocumentDto>> Handle(
        ListSubcontractorDocumentsQuery req, CancellationToken ct)
    {
        await accessService.EnsureSubcontractorVisibleAsync(req.SubcontractorId, ct);

        var rows = await db.SubcontractorDocuments.AsNoTracking()
            .Where(d => d.SubcontractorId == req.SubcontractorId)
            .OrderByDescending(d => d.UploadedAt)
            .Join(
                db.Users.AsNoTracking(),
                d => d.UploadedByUserId,
                u => u.Id,
                (d, u) => new { d, u.FullName })
            .ToListAsync(ct);

        return rows.Select(x => new SubcontractorDocumentDto(
            x.d.Id,
            x.d.Name,
            x.d.DocumentType,
            SubcontractorDocumentType.Label(x.d.DocumentType),
            x.d.ContentType,
            x.d.UploadedAt,
            x.FullName)).ToList();
    }
}

public sealed record UploadSubcontractorDocumentCommand(
    Guid SubcontractorId,
    string Name,
    string DocumentType,
    string FileName,
    Stream Content) : IRequest<SubcontractorDocumentDto>;

public sealed class UploadSubcontractorDocumentHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    ISubcontractorDocumentStorage storage) : IRequestHandler<UploadSubcontractorDocumentCommand, SubcontractorDocumentDto>
{
    private const int MaxBytes = 15 * 1024 * 1024;

    public async Task<SubcontractorDocumentDto> Handle(
        UploadSubcontractorDocumentCommand req, CancellationToken ct)
    {
        await accessService.EnsureSubcontractorVisibleAsync(req.SubcontractorId, ct);
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access,
            p => p.CanRegisterSubcontractors || p.CanUploadDocuments || p.IsGlobalAdmin,
            "Нет прав на загрузку документов субподрядчика.");
        accessService.EnsureCanModify(access);

        if (!await db.Subcontractors.AnyAsync(s => s.Id == req.SubcontractorId, ct))
            throw new NotFoundException("Subcontractor", req.SubcontractorId);

        var docType = req.DocumentType.Trim().ToLowerInvariant();
        if (!SubcontractorDocumentType.All.Contains(docType))
            throw new ValidationFailedException("Недопустимый тип документа.");

        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationFailedException("Укажите наименование документа.");

        var (contentType, ext) = ProjectDocumentContent.Resolve(req.FileName);
        if (contentType is null)
            throw new ValidationFailedException("Допустимые форматы: PDF, JPG, PNG, DOCX, XLSX.");

        if (req.Content.CanSeek && req.Content.Length > MaxBytes)
            throw new ValidationFailedException("Файл больше 15 МБ.");

        var uploaderId = currentUser.UserId ?? throw new UnauthorizedException();
        var entity = new SubcontractorDocument
        {
            SubcontractorId = req.SubcontractorId,
            Name = name,
            DocumentType = docType,
            ContentType = contentType,
            UploadedByUserId = uploaderId
        };

        entity.FilePath = await storage.SaveAsync(
            req.SubcontractorId, entity.Id, $"{name}{ext}", req.Content, ct);
        db.SubcontractorDocuments.Add(entity);
        await db.SaveChangesAsync(ct);

        var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == uploaderId, ct);
        return new SubcontractorDocumentDto(
            entity.Id,
            entity.Name,
            entity.DocumentType,
            SubcontractorDocumentType.Label(entity.DocumentType),
            entity.ContentType,
            entity.UploadedAt,
            uploader.FullName);
    }
}

internal static class ProjectDocumentContent
{
    public static (string? ContentType, string Extension) Resolve(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ("application/pdf", ext),
            ".jpg" or ".jpeg" => ("image/jpeg", ext),
            ".png" => ("image/png", ext),
            ".docx" => ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", ext),
            ".xlsx" => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ext),
            _ => (null, ext)
        };
    }
}

public sealed record DeleteSubcontractorDocumentCommand(Guid SubcontractorId, Guid DocumentId) : IRequest<Unit>;

public sealed class DeleteSubcontractorDocumentHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    ISubcontractorDocumentStorage storage) : IRequestHandler<DeleteSubcontractorDocumentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteSubcontractorDocumentCommand req, CancellationToken ct)
    {
        await accessService.EnsureSubcontractorVisibleAsync(req.SubcontractorId, ct);
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access,
            p => p.CanRegisterSubcontractors || p.IsGlobalAdmin,
            "Нет прав на удаление документов субподрядчика.");
        accessService.EnsureCanModify(access);

        var doc = await db.SubcontractorDocuments
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.SubcontractorId == req.SubcontractorId, ct)
            ?? throw new NotFoundException("SubcontractorDocument", req.DocumentId);

        db.SubcontractorDocuments.Remove(doc);
        await db.SaveChangesAsync(ct);
        await storage.DeleteAsync(doc.FilePath, ct);
        return Unit.Value;
    }
}

public sealed record GetSubcontractorDocumentFileQuery(Guid SubcontractorId, Guid DocumentId)
    : IRequest<(Stream Stream, string FileName, string ContentType)?>;

public sealed class GetSubcontractorDocumentFileHandler(
    ITansuDbContext db,
    ITansuAccessService accessService,
    ISubcontractorDocumentStorage storage) : IRequestHandler<GetSubcontractorDocumentFileQuery, (Stream, string, string)?>
{
    public async Task<(Stream, string, string)?> Handle(
        GetSubcontractorDocumentFileQuery req, CancellationToken ct)
    {
        await accessService.EnsureSubcontractorVisibleAsync(req.SubcontractorId, ct);

        var doc = await db.SubcontractorDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.SubcontractorId == req.SubcontractorId, ct);
        if (doc is null) return null;

        var stream = await storage.OpenReadAsync(doc.FilePath, ct);
        if (stream is null) return null;

        var ext = Path.GetExtension(doc.FilePath);
        var fileName = doc.Name.Contains('.') ? doc.Name : doc.Name + ext;
        return (stream, fileName, doc.ContentType ?? "application/octet-stream");
    }
}
