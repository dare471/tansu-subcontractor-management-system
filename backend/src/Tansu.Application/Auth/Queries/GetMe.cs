using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.Auth.Queries;

public sealed record GetMeQuery : IRequest<MeResponse>;

public sealed class GetMeHandler(
    ITansuDbContext db,
    ICurrentUser currentUser) : IRequestHandler<GetMeQuery, MeResponse>
{
    public async Task<MeResponse> Handle(GetMeQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException();
        var user = await db.Users.AsNoTracking()
            .Include(u => u.Subcontractor)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException();

        return new MeResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Position,
            user.UserType,
            user.SubcontractorId,
            user.Subcontractor?.Name,
            user.Subcontractor?.Bin,
            user.ApproverRole,
            user.MustChangePassword,
            user.EmployeeId);
    }
}
