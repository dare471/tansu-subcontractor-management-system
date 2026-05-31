using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.Employees;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Subcontractors.Commands;

public sealed record UpdateSubcontractorCommand(Guid Id, string Name, string Bin) : IRequest<SubcontractorDto>;

public sealed class UpdateSubcontractorValidator : AbstractValidator<UpdateSubcontractorCommand>
{
    public UpdateSubcontractorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Bin).NotEmpty().MaximumLength(32).Matches("^[0-9]+$");
    }
}

public sealed class UpdateSubcontractorHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<UpdateSubcontractorCommand, SubcontractorDto>
{
    public async Task<SubcontractorDto> Handle(UpdateSubcontractorCommand req, CancellationToken ct)
    {
        await accessService.EnsureSubcontractorVisibleAsync(req.Id, ct);
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanRegisterSubcontractors || p.IsGlobalAdmin,
            "Нет права редактировать субподрядчиков.");

        var entity = await db.Subcontractors
            .Include(x => x.Projects)
            .FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Subcontractor", req.Id);

        if (entity.Bin != req.Bin &&
            await db.Subcontractors.AnyAsync(x => x.Bin == req.Bin && x.Id != req.Id, ct))
        {
            throw new ConflictException("bin_taken", "Другой субподрядчик уже имеет такой БИН.");
        }

        entity.Name = req.Name.Trim();
        entity.Bin = req.Bin.Trim();
        await db.SaveChangesAsync(ct);

        var employeeIds = await db.Employees.AsNoTracking()
            .Where(e => e.SubcontractorId == entity.Id)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var approved = 0;
        var notApproved = 0;

        if (employeeIds.Count > 0)
        {
            var sheets = await db.ApprovalSheet.AsNoTracking()
                .Where(a => employeeIds.Contains(a.EmployeeId))
                .ToListAsync(ct);

            var sheetsByEmployee = sheets
                .GroupBy(s => s.EmployeeId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ApprovalSheetEntry>)g.ToList());

            foreach (var employeeId in employeeIds)
            {
                sheetsByEmployee.TryGetValue(employeeId, out var employeeSheets);
                employeeSheets ??= Array.Empty<ApprovalSheetEntry>();
                var status = EmployeeStatusResolver.ResolveFromSheets(employeeSheets);

                if (status == ApprovalStatus.Approved)
                    approved++;
                else
                    notApproved++;
            }
        }

        return new SubcontractorDto(
            entity.Id, entity.Name, entity.Bin,
            entity.Projects.Count, approved, notApproved, entity.IsActive, entity.CreatedAt);
    }
}
