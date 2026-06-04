using Microsoft.EntityFrameworkCore;
using Tansu.Application.Auth;
using Tansu.Application.Common.Exceptions;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Application.Users;

internal static class UserManagementAccess
{
    public static void EnsureList(TansuAccessContext access, string? userTypeFilter)
    {
        if (access.Permissions.IsGlobalAdmin || access.Permissions.CanManageTansuUsers)
            return;

        if (access.Permissions.CanManageApprovalMatrix &&
            (userTypeFilter is null or UserType.Tansu))
            return;

        if (access.Permissions.CanManageSubcontractorUsers &&
            (userTypeFilter is null or UserType.Subcontractor))
            return;

        throw new ForbiddenException("Нет доступа к списку пользователей.");
    }

    public static void EnsureCreate(
        TansuAccessContext access,
        string userType,
        bool isGlobalAdminFlow)
    {
        if (userType == UserType.Tansu)
        {
            if (!access.Permissions.CanManageTansuUsers && !access.Permissions.IsGlobalAdmin)
                throw new ForbiddenException("Создание пользователей ТАНСУ доступно глобальному администратору.");
            if (!isGlobalAdminFlow && !access.Permissions.IsGlobalAdmin)
                throw new ForbiddenException("Укажите компанию и сотрудника из ЗУП.");
            return;
        }

        if (userType == UserType.Subcontractor)
        {
            if (access.Permissions.CanManageSubcontractorUsers ||
                access.Permissions.CanManageTansuUsers ||
                access.Permissions.IsGlobalAdmin)
                return;
        }

        throw new ForbiddenException("Нет прав на создание пользователя.");
    }

    public static void EnsureManageUser(TansuAccessContext access, User user, Guid? currentUserId)
    {
        if (access.Permissions.IsGlobalAdmin || access.Permissions.CanManageTansuUsers)
            return;

        if (user.UserType == UserType.Employee)
            throw new ForbiddenException("Управление личными кабинетами сотрудников — только глобальный администратор.");

        if (user.UserType == UserType.Tansu)
        {
            if (access.Permissions.CanManageSubordinates && currentUserId is not null)
            {
                // Директор может менять подчинённых (упрощённо — только через глобального админа в UI)
            }

            throw new ForbiddenException("Редактирование пользователей ТАНСУ доступно глобальному администратору.");
        }

        if (user.UserType == UserType.Subcontractor && access.Permissions.CanManageSubcontractorUsers)
            return;

        throw new ForbiddenException("Нет прав на управление этим пользователем.");
    }

    public static async Task EnsureSubcontractorOwnedByManagerAsync(
        ITansuDbContext db,
        Guid subcontractorId,
        Guid managerUserId,
        CancellationToken ct)
    {
        var owned = await db.Subcontractors.AsNoTracking()
            .AnyAsync(s => s.Id == subcontractorId && s.RegisteredByUserId == managerUserId, ct);

        if (!owned)
            throw new ForbiddenException("Можно создавать пользователей только для организаций, которые вы зарегистрировали.");
    }
}
