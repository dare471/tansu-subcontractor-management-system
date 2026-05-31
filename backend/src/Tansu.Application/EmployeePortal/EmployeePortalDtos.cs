namespace Tansu.Application.EmployeePortal;

public sealed record EmployeePortalDashboardDto(
    Guid EmployeeId,
    string FullName,
    string Position,
    string WorkDescription,
    string SubcontractorName,
    string? ProjectName,
    string? ApprovalStatus,
    bool IsApproved,
    bool SafetyQuizCompleted,
    int? SafetyQuizScore,
    int? SafetyQuizTotal,
    bool CanShowQrPass,
    EmployeePortalPassDto? AccessPass,
    bool HasHelmet,
    bool HasUniform);

public sealed record EmployeePortalProfileDto(
    Guid EmployeeId,
    string FullName,
    string Position,
    string Phone,
    string Iin,
    string SubcontractorName,
    string? ProjectName,
    string? ApprovalStatus,
    bool HasPhoto,
    string? PhotoReviewStatus,
    string? PhotoReviewReason,
    DateTimeOffset? AccessPassIssuedAt);

public sealed record EmployeePortalSiteVisitsDto(
    IReadOnlyList<EmployeePortalSiteVisitItemDto> Visits,
    DateTimeOffset? LastCheckedInAt,
    int TotalCount);

public sealed record EmployeePortalSiteVisitItemDto(
    Guid Id,
    string? ProjectName,
    DateTimeOffset CheckedInAt,
    double? FaceConfidence,
    string VerificationMethod);

public sealed record EmployeePortalPhotoUploadResult(string PhotoPath, string Message);

public sealed record EmployeePortalPassDto(
    Guid Id,
    string VerifyUrl,
    DateTimeOffset IssuedAt,
    bool HasReferencePhoto);

public sealed record SafetyQuizQuestionDto(
    string Id,
    string Text,
    IReadOnlyList<SafetyQuizOptionDto> Options);

public sealed record SafetyQuizOptionDto(string Id, string Text);

public sealed record SafetyQuizSubmitRequest(IReadOnlyDictionary<string, string> Answers);

public sealed record SafetyQuizSubmitResult(
    bool Passed,
    int Score,
    int Total,
    string Message);

public sealed record EmployeeLoginRequest(string Iin, string Password);
