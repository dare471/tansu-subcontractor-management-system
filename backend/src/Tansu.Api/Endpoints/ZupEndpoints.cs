using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Zup.Queries;

namespace Tansu.Api.Endpoints;

public static class ZupEndpoints
{
    public static IEndpointRouteBuilder MapZupEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/zup")
            .WithTags("Zup")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("/employees", async (
            [FromQuery] string company,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListZupEmployeesQuery(company), ct)));

        g.MapGet("/projects", async (IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new ListZupProjectsQuery(), ct)));

        return app;
    }
}
