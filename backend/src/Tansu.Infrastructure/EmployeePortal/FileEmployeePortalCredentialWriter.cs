using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tansu.Application.Common.Interfaces;
using Tansu.Application.EmployeePortal;

namespace Tansu.Infrastructure.EmployeePortal;

public sealed class FileEmployeePortalCredentialWriter(
    IOptions<EmployeePortalOptions> options,
    ILogger<FileEmployeePortalCredentialWriter> logger) : IEmployeePortalCredentialWriter
{
    public async Task WriteAsync(
        Guid employeeId,
        string fullName,
        string iin,
        string oneTimePassword,
        CancellationToken cancellationToken = default)
    {
        var path = options.Value.CredentialsLogPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var line =
                $"{DateTimeOffset.UtcNow:O}\t{employeeId}\t{fullName}\tIIN={iin}\tOTP={oneTimePassword}{Environment.NewLine}";
            await File.AppendAllTextAsync(path, line, cancellationToken);
            logger.LogInformation(
                "Личный кабинет: одноразовый пароль записан в {Path} (dev) для {EmployeeId}",
                path,
                employeeId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось записать OTP личного кабинета в файл {Path}", path);
        }
    }
}
