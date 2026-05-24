using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Matrix;
using Tansu.Application.Matrix.Commands;
using Tansu.Application.Matrix.Queries;

namespace Tansu.Api.Endpoints;

public static class MatrixEndpoints
{
    public static IEndpointRouteBuilder MapMatrixEndpoints(this IEndpointRouteBuilder app)
    {
        var list = app.MapGroup("/api/approval-matrix")
            .WithTags("ApprovalMatrix")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        list.MapGet("", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListMatricesQuery(), ct)));

        var g = app.MapGroup("/api/projects/{projectOid:guid}/subcontractors/{subcontractorId:guid}/matrix")
            .WithTags("ApprovalMatrix")
            .RequireAuthorization(AuthPolicies.TansuOnly);

        g.MapGet("", async (
            Guid projectOid, Guid subcontractorId,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(new GetMatrixQuery(projectOid, subcontractorId), ct)));

        g.MapPut("", async (
            Guid projectOid, Guid subcontractorId,
            [FromBody] SetMatrixRequest req,
            IMediator m, CancellationToken ct) =>
                Results.Ok(await m.Send(
                    new SetMatrixCommand(projectOid, subcontractorId, req.Steps), ct)));

        return app;
    }
}
