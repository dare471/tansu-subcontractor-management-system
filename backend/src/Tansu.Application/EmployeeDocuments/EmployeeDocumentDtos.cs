namespace Tansu.Application.EmployeeDocuments;

public sealed record EmployeeDocumentDto(
    Guid Id,
    Guid EmployeeId,
    string Name,
    string DocumentType,
    string DocumentTypeLabel,
    string FilePath,
    string? ContentType,
    DateTimeOffset UploadedAt,
    DateTimeOffset? ExpiresAt,
    Guid UploadedByUserId,
    string UploadedByFullName,
    bool IsExpired,
    bool IsExpiringSoon,
    Guid? SupersedesDocumentId,
    bool IsSuperseded,
    int VersionNo);

public sealed record EmployeeDocumentsSummaryDto(
    IReadOnlyList<EmployeeDocumentDto> Documents,
    int TotalCount,
    int ExpiringWithin14Days);

public sealed record UploadEmployeeDocumentRequest(
    string Name,
    string DocumentType,
    DateTimeOffset? ExpiresAt,
    Guid? ReplacesDocumentId);

public sealed record EmployeeBlockRecordDto(
    Guid Id,
    Guid EmployeeId,
    Guid InitiatedByUserId,
    string InitiatedByFullName,
    string? InitiatorRole,
    string? InitiatorRoleLabel,
    string ActionType,
    string Reason,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record EmployeeBlockStatusDto(
    bool IsBlocked,
    EmployeeBlockRecordDto? LastRecord,
    IReadOnlyList<EmployeeBlockRecordDto> History);

public sealed record BlockEmployeeRequest(string Reason);
