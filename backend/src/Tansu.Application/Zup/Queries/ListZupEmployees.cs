using MediatR;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Zup.Queries;

public sealed record ListZupEmployeesQuery(string EmployerCompany) : IRequest<IReadOnlyList<ZupEmployeeDto>>;

public sealed class ListZupEmployeesHandler(
    IZupEmployeeDirectory directory,
    ITansuAccessService accessService) : IRequestHandler<ListZupEmployeesQuery, IReadOnlyList<ZupEmployeeDto>>
{
    public async Task<IReadOnlyList<ZupEmployeeDto>> Handle(ListZupEmployeesQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access,
            p => p.CanManageTansuUsers || p.IsGlobalAdmin,
            "Справочник ЗУП доступен только глобальному администратору.");

        if (!TansuEmployerCompany.IsValid(req.EmployerCompany))
            throw new ValidationFailedException("Укажите компанию ТАНСУ.");

        return await directory.ListAsync(req.EmployerCompany, ct);
    }
}
