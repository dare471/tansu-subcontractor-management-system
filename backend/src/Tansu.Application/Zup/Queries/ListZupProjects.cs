using MediatR;
using Tansu.Application.Auth;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Zup.Queries;

public sealed record ListZupProjectsQuery : IRequest<IReadOnlyList<ZupProjectDto>>;

public sealed class ListZupProjectsHandler(
    IZupProjectDirectory directory,
    ITansuAccessService accessService) : IRequestHandler<ListZupProjectsQuery, IReadOnlyList<ZupProjectDto>>
{
    public async Task<IReadOnlyList<ZupProjectDto>> Handle(ListZupProjectsQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access,
            p => p.CanViewProjects || p.CanManageProjects || p.IsGlobalAdmin,
            "Справочник проектов ЗУП недоступен для вашей роли.");

        return await directory.ListAsync(ct);
    }
}
