using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Projects;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Projects.Commands;

public sealed record UploadProjectDocumentCommand(
    Guid ProjectOid,
    string Name,
    string DocumentType,
    string FileName,
    Stream Content) : IRequest<ProjectDocumentDto>;

public sealed class UploadProjectDocumentHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IProjectDocumentStorage storage) : IRequestHandler<UploadProjectDocumentCommand, ProjectDocumentDto>
{
    private const int MaxBytes = 15 * 1024 * 1024;

    public async Task<ProjectDocumentDto> Handle(UploadProjectDocumentCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanManageProjects || p.IsGlobalAdmin,
            "Нет прав на загрузку документов проекта.");
        accessService.EnsureCanModify(access);

        if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
            throw new ForbiddenException("Проект вне вашей области видимости.");

        if (!await db.ProjectRefs.AnyAsync(p => p.ProjectOid == req.ProjectOid, ct))
            throw new NotFoundException("Project", req.ProjectOid);

        var docType = req.DocumentType.Trim().ToLowerInvariant();
        if (!ProjectDocumentType.All.Contains(docType))
            throw new ValidationFailedException("Недопустимый тип документа.");

        var name = req.Name.Trim();
        if (string.IsNullOrEmpty(name))
            throw new ValidationFailedException("Укажите наименование документа.");

        var (contentType, ext) = ResolveContentType(req.FileName);
        if (contentType is null)
            throw new ValidationFailedException("Допустимые форматы: PDF, JPG, PNG, DOCX, XLSX.");

        if (req.Content.CanSeek && req.Content.Length > MaxBytes)
            throw new ValidationFailedException("Файл больше 15 МБ.");

        var uploaderId = currentUser.UserId ?? throw new UnauthorizedException();
        var entity = new ProjectDocument
        {
            ProjectOid = req.ProjectOid,
            Name = name,
            DocumentType = docType,
            ContentType = contentType,
            UploadedByUserId = uploaderId
        };

        entity.FilePath = await storage.SaveAsync(req.ProjectOid, entity.Id, $"{name}{ext}", req.Content, ct);
        db.ProjectDocuments.Add(entity);
        await db.SaveChangesAsync(ct);

        var uploader = await db.Users.AsNoTracking().FirstAsync(u => u.Id == uploaderId, ct);
        return new ProjectDocumentDto(
            entity.Id,
            entity.Name,
            entity.DocumentType,
            ProjectDocumentType.Label(entity.DocumentType),
            entity.ContentType,
            entity.UploadedAt,
            uploader.FullName);
    }

    internal static (string? ContentType, string Extension) ResolveContentType(string fileName)
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

public sealed record DeleteProjectDocumentCommand(Guid ProjectOid, Guid DocumentId) : IRequest<Unit>;

public sealed class DeleteProjectDocumentHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IProjectDocumentStorage storage) : IRequestHandler<DeleteProjectDocumentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProjectDocumentCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanManageProjects || p.IsGlobalAdmin,
            "Нет прав на удаление документов проекта.");
        accessService.EnsureCanModify(access);

        var doc = await db.ProjectDocuments
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.ProjectOid == req.ProjectOid, ct)
            ?? throw new NotFoundException("ProjectDocument", req.DocumentId);

        db.ProjectDocuments.Remove(doc);
        await db.SaveChangesAsync(ct);
        await storage.DeleteAsync(doc.FilePath, ct);
        return Unit.Value;
    }
}

public sealed record GetProjectDocumentFileQuery(Guid ProjectOid, Guid DocumentId)
    : IRequest<(Stream Stream, string FileName, string ContentType)?>;

public sealed class GetProjectDocumentFileHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IProjectDocumentStorage storage) : IRequestHandler<GetProjectDocumentFileQuery, (Stream, string, string)?>
{
    public async Task<(Stream, string, string)?> Handle(GetProjectDocumentFileQuery req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
            throw new ForbiddenException("Проект вне вашей области видимости.");

        var doc = await db.ProjectDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == req.DocumentId && d.ProjectOid == req.ProjectOid, ct);
        if (doc is null) return null;

        var stream = await storage.OpenReadAsync(doc.FilePath, ct);
        if (stream is null) return null;

        var ext = Path.GetExtension(doc.FilePath);
        var fileName = doc.Name.Contains('.') ? doc.Name : doc.Name + ext;
        return (stream, fileName, doc.ContentType ?? "application/octet-stream");
    }
}
