namespace Tansu.Application.AccessPasses;

public sealed record EmployeeAccessPassDto(
    Guid Id,
    Guid EmployeeId,
    string Token,
    string VerifyUrl,
    DateTimeOffset IssuedAt,
    bool HasReferencePhoto);

public sealed record AccessPassLookupDto(
    Guid EmployeeId,
    string FullName,
    string Position,
    string SubcontractorName,
    string? ProjectName,
    bool HasReferencePhoto,
    DateTimeOffset IssuedAt,
    bool IsActive);

public sealed record FaceVerificationResultDto(
    bool Matched,
    double Confidence,
    string Message,
    AccessPassLookupDto? Employee);

public sealed record EmployeeSiteVisitDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeFullName,
    string? ProjectName,
    DateTimeOffset CheckedInAt,
    double? FaceConfidence,
    string VerificationMethod);
