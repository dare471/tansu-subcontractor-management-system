using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.EmployeeDocuments.Commands;

public sealed record UploadEmployeeDocumentCommand(
    Guid EmployeeId,
    string Name,
    string DocumentType,
    DateTimeOffset? ExpiresAt,
    string FileName,
    Stream Content,
    Guid? ReplacesDocumentId = null) : IRequest<EmployeeDocumentDto>;

public sealed class UploadEmployeeDocumentHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IEmployeeDocumentStorage storage) : IRequestHandler<UploadEmployeeDocumentCommand, EmployeeDocumentDto>
{
    private const int MaxBytes = 10 * 1024 * 1024;

    public async Task<EmployeeDocumentDto> Handle(UploadEmployeeDocumentCommand req, CancellationToken ct)
    {
        await EmployeeDocumentAuthorization.EnsureEmployeeAccessAsync(
            req.EmployeeId, currentUser, db, accessService, ct, writeAccess: true);

        if (currentUser.UserType == UserType.Employee)
            throw new ForbiddenException();

        var docType = req.DocumentType.Trim().ToLowerInvariant();
        if (!EmployeeDocumentType.All.Contains(docType))
            throw new ValidationFailedException("Недопустимый тип документа.");

        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationFailedException("Укажите наименование документа.");

        var (contentType, ext) = ResolveContentType(req.FileName);
        if (contentType is null)
        {
            throw new ValidationFailedException(
                "Допустимые форматы: PDF, JPG, PNG.");
        }

        if (req.Content.CanSeek && req.Content.Length > MaxBytes)
            throw new ValidationFailedException("Файл больше 10 МБ.");

        if (req.ReplacesDocumentId is { } replacesId)
        {
            var existing = await db.EmployeeDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == replacesId && d.EmployeeId == req.EmployeeId, ct);
            if (existing is null)
                throw new NotFoundException("EmployeeDocument", replacesId);
        }

        var uploaderId = currentUser.UserId ?? throw new UnauthorizedException();

        var entity = new EmployeeDocument
        {
            EmployeeId = req.EmployeeId,
            Name = name,
            DocumentType = docType,
            ExpiresAt = req.ExpiresAt,
            UploadedByUserId = uploaderId,
            SupersedesDocumentId = req.ReplacesDocumentId,
            ContentType = contentType
        };

        entity.FilePath = await storage.SaveAsync(req.EmployeeId, entity.Id, $"{name}{ext}", req.Content, ct);
        db.EmployeeDocuments.Add(entity);
        await db.SaveChangesAsync(ct);

        var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == uploaderId, ct);
        entity.UploadedBy = uploader;

        var supersededIds = await LoadSupersededIdsAsync(db, entity.Id, ct);
        return EmployeeDocumentMapper.ToDto(entity, DateTimeOffset.UtcNow, supersededIds);
    }

    internal static (string? ContentType, string Extension) ResolveContentType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ("application/pdf", ext),
            ".jpg" or ".jpeg" => ("image/jpeg", ext == ".jpeg" ? ".jpg" : ext),
            ".png" => ("image/png", ext),
            _ => (null, ext)
        };
    }

    internal static async Task<HashSet<Guid>> LoadSupersededIdsAsync(
        ITansuDbContext db,
        Guid employeeId,
        CancellationToken ct)
    {
        var rows = await db.EmployeeDocuments.AsNoTracking()
            .Where(d => d.EmployeeId == employeeId && d.SupersedesDocumentId != null)
            .Select(d => new { d.Id, d.SupersedesDocumentId })
            .ToListAsync(ct);

        var superseded = new HashSet<Guid>();
        foreach (var row in rows)
        {
            if (row.SupersedesDocumentId is { } prev)
                superseded.Add(prev);
        }

        return superseded;
    }
}

public sealed record DeleteEmployeeDocumentCommand(Guid EmployeeId, Guid DocumentId) : IRequest<Unit>;

public sealed class DeleteEmployeeDocumentHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IEmployeeDocumentStorage storage) : IRequestHandler<DeleteEmployeeDocumentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteEmployeeDocumentCommand req, CancellationToken ct)
    {
        await EmployeeDocumentAuthorization.EnsureEmployeeAccessAsync(
            req.EmployeeId, currentUser, db, accessService, ct, writeAccess: true);

        if (currentUser.UserType == UserType.Employee)
            throw new ForbiddenException();

        var doc = await db.EmployeeDocuments
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.EmployeeId == req.EmployeeId, ct)
            ?? throw new NotFoundException("EmployeeDocument", req.DocumentId);

        var hasSuccessor = await db.EmployeeDocuments.AnyAsync(
            d => d.SupersedesDocumentId == doc.Id, ct);
        if (hasSuccessor)
        {
            throw new ConflictException("document_has_versions",
                "Нельзя удалить документ, на который загружена новая версия.");
        }

        await storage.DeleteAsync(doc.FilePath, ct);
        db.EmployeeDocuments.Remove(doc);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
