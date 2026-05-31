using MediatR;
using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users.Queries;

public sealed record GetUserBlockStatusQuery(Guid UserId) : IRequest<UserBlockStatusDto>;

public sealed class GetUserBlockStatusHandler(
    ITansuDbContext db,
    ITansuAccessService accessService) : IRequestHandler<GetUserBlockStatusQuery, UserBlockStatusDto>
{
    public async Task<UserBlockStatusDto> Handle(GetUserBlockStatusQuery req, CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access, p => p.CanManageTansuUsers, "Управление пользователями доступно только глобальному администратору.");

        if (!await db.Users.AnyAsync(u => u.Id == req.UserId, ct))
            throw new NotFoundException("User", req.UserId);

        var rows = await db.UserBlockRecords.AsNoTracking()
            .Where(r => r.UserId == req.UserId)
            .Include(r => r.InitiatedBy)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        var history = rows.Select(UserBlockMapper.ToDto).ToList();
        var last = history.FirstOrDefault();
        var isBlocked = last?.ActionType == EmployeeBlockActionType.Block;

        return new UserBlockStatusDto(isBlocked, last, history);
    }
}

internal static class UserBlockMapper
{
    public static UserBlockRecordDto ToDto(Domain.Entities.UserBlockRecord row) =>
        new(
            row.Id,
            row.UserId,
            row.InitiatedByUserId,
            row.InitiatedBy?.FullName ?? row.InitiatedByUserId.ToString(),
            row.ActionType,
            row.Reason,
            row.CreatedAt);
}
