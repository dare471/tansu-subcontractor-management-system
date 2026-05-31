using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Projects.Queries;
using Tansu.Domain.Enums;

namespace Tansu.Application.Projects.Commands;

public sealed record UpdateProjectCommand(Guid ProjectOid, UpdateProjectRequest Body) : IRequest<ProjectDetailDto>;

public sealed class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Body.CustomerEmail)
            .EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Body.CustomerEmail))
            .WithMessage("Некорректный email заказчика.");
    }
}

public sealed class UpdateProjectHandler(
    ITansuDbContext db,
    ICurrentUser currentUser,
    ITansuAccessService accessService,
    IMediator mediator) : IRequestHandler<UpdateProjectCommand, ProjectDetailDto>
{
    public async Task<ProjectDetailDto> Handle(UpdateProjectCommand req, CancellationToken ct)
    {
        if (currentUser.UserType != UserType.Tansu)
            throw new ForbiddenException();

        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanRegisterSubcontractors || p.IsGlobalAdmin,
            "Нет прав на редактирование проекта.");

        if (access.VisibleProjectOids is { } projects && !projects.Contains(req.ProjectOid))
            throw new ForbiddenException("Проект вне вашей области видимости.");

        var project = await db.ProjectRefs
            .FirstOrDefaultAsync(p => p.ProjectOid == req.ProjectOid, ct)
            ?? throw new NotFoundException("Project", req.ProjectOid);

        var body = req.Body;
        if (body.Name is not null) project.Name = string.IsNullOrWhiteSpace(body.Name) ? null : body.Name.Trim();
        if (body.CustomerName is not null)
            project.CustomerName = string.IsNullOrWhiteSpace(body.CustomerName) ? null : body.CustomerName.Trim();
        if (body.CustomerPhone is not null)
            project.CustomerPhone = string.IsNullOrWhiteSpace(body.CustomerPhone) ? null : body.CustomerPhone.Trim();
        if (body.CustomerEmail is not null)
            project.CustomerEmail = string.IsNullOrWhiteSpace(body.CustomerEmail) ? null : body.CustomerEmail.Trim();
        if (body.BudgetAmount is not null) project.BudgetAmount = body.BudgetAmount;
        if (body.BudgetCurrency is not null)
            project.BudgetCurrency = string.IsNullOrWhiteSpace(body.BudgetCurrency) ? "KZT" : body.BudgetCurrency.Trim();

        if (body.ResponsibleAdminUserId is { } adminId)
        {
            if (!await db.Users.AnyAsync(u => u.Id == adminId && u.UserType == UserType.Tansu, ct))
                throw new NotFoundException("User", adminId);
            project.ResponsibleAdminUserId = adminId;
        }
        else
            project.ResponsibleAdminUserId = null;

        if (body.ProjectManagerUserId is { } pmId)
        {
            if (!await db.Users.AnyAsync(u => u.Id == pmId && u.UserType == UserType.Tansu, ct))
                throw new NotFoundException("User", pmId);
            project.ProjectManagerUserId = pmId;
        }
        else
            project.ProjectManagerUserId = null;

        await db.SaveChangesAsync(ct);
        return await mediator.Send(new GetProjectDetailQuery(req.ProjectOid), ct);
    }
}
