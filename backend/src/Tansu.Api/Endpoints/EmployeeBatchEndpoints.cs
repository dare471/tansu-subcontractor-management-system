using MediatR;
using Microsoft.AspNetCore.Mvc;
using Tansu.Api.Auth;
using Tansu.Application.Approvals;
using Tansu.Application.Approvals.Commands;

namespace Tansu.Api.Endpoints;

public static class EmployeeBatchEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeBatchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/employee-batches")
            .WithTags("Employee Batches")
            .RequireAuthorization(AuthPolicies.SubcontractorOnly);

        g.MapGet("", async (IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new ListEmployeeBatchesQuery(), ct)));

        g.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new GetEmployeeBatchQuery(id), ct)));

        g.MapPost("", async (
            [FromBody] CreateEmployeeBatchRequest req,
            IMediator m, CancellationToken ct) =>
        {
            var dto = await m.Send(new CreateEmployeeBatchCommand(req.ProjectOid, req.Title), ct);
            return Results.Created($"/api/employee-batches/{dto.Id}", dto);
        });

        g.MapPost("/{id:guid}/members", async (
            Guid id,
            [FromBody] AddEmployeesToBatchRequest req,
            IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new AddEmployeesToBatchCommand(id, req.EmployeeIds), ct)));

        g.MapDelete("/{id:guid}/members/{employeeId:guid}", async (
            Guid id, Guid employeeId,
            IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new RemoveEmployeeFromBatchCommand(id, employeeId), ct)));

        g.MapPost("/{id:guid}/submit", async (
            Guid id, IMediator m, CancellationToken ct) =>
            Results.Ok(await m.Send(new SubmitEmployeeBatchCommand(id), ct)));

        g.MapDelete("/{id:guid}", async (
            Guid id, IMediator m, CancellationToken ct) =>
        {
            await m.Send(new DeleteEmployeeBatchCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
