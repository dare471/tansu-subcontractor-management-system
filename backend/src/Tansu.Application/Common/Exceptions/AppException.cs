namespace Tansu.Application.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public sealed class NotFoundException(string entity, object key)
    : AppException("not_found", $"{entity} '{key}' not found.", 404);

public sealed class ConflictException(string code, string message)
    : AppException(code, message, 409);

public sealed class UnauthorizedException(string message = "Unauthorized.")
    : AppException("unauthorized", message, 401);

public sealed class ForbiddenException(string message = "Forbidden.")
    : AppException("forbidden", message, 403);

public sealed class ValidationFailedException(string message)
    : AppException("validation_failed", message, 400);
