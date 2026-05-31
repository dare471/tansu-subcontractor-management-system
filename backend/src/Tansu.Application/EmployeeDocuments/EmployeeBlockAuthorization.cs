using Tansu.Application.Common.Interfaces;

namespace Tansu.Application.EmployeeDocuments;

internal static class EmployeeBlockAuthorization
{
    public static async Task EnsureCanInitiateBlockAsync(
        ITansuAccessService accessService,
        CancellationToken ct)
    {
        var access = await accessService.GetAccessAsync(ct);
        accessService.EnsurePermission(
            access,
            p => p.CanBlockEmployees,
            "Блокировку могут инициировать пользователи с ролями ОИД, БиОТ/ТБ, СБ или руководитель проекта.");
    }
}
