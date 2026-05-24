namespace Tansu.Application.DocumentRequests;

public sealed record DocumentRequestDto(
    Guid Id,
    Guid SubcontractorId,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    string RequestType,
    string Title,
    string Description,
    string? CurrentStatus,
    string? PendingApproverFullName,
    string? PendingApproverRole,
    int? PendingStepNo,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DocumentRequestInboxItemDto(
    Guid SheetId,
    Guid RequestId,
    string RequestType,
    string Title,
    string SubcontractorName,
    Guid ProjectOid,
    string? ProjectName,
    string ApproverRole,
    int OrderNo,
    DateTimeOffset SubmittedAt);

public sealed record DocumentApprovalStepDto(
    Guid Id,
    int OrderNo,
    string ApproverRole,
    string ApproverFullName,
    string ApproverEmail,
    string Status,
    string? Comment,
    DateTimeOffset? DecidedAt);

public sealed record DocumentApprovalRoundDto(
    Guid RoundId,
    DateTimeOffset StartedAt,
    IReadOnlyList<DocumentApprovalStepDto> Steps);

public sealed record CreateDocumentRequestRequest(
    Guid ProjectOid,
    string RequestType,
    string Title,
    string Description);

public sealed record UpdateDocumentRequestRequest(string Title, string Description);

public sealed record DocumentMatrixStepDto(
    Guid Id,
    int OrderNo,
    string ApproverRole);

public sealed record DocumentMatrixSummaryDto(
    Guid ProjectOid,
    string? ProjectName,
    Guid SubcontractorId,
    string SubcontractorName,
    string RequestType,
    IReadOnlyList<DocumentMatrixStepDto> Steps);

public sealed record SetDocumentMatrixRequest(IReadOnlyList<DocumentMatrixStepInput> Steps);

public sealed record DocumentMatrixStepInput(int OrderNo, string ApproverRole);

public static class DocumentRequestLabels
{
    public static string RequestType(string type) => type switch
    {
        "leave" => "Отпуск",
        "ticket" => "Тикет / обращение",
        "document" => "Документ",
        "expense" => "Расход / финансы",
        _ => type
    };

    public static string ApproverRole(string role) => role switch
    {
        "accounting" => "Бухгалтерия",
        "hr" => "Кадры",
        "finance" => "Финансы",
        "management" => "Руководство",
        _ => role
    };
}
