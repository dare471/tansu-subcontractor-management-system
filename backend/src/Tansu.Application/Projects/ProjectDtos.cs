namespace Tansu.Application.Projects;

public sealed record ProjectDto(
    Guid ProjectOid,
    int? ZupId,
    string? Code,
    string? Name,
    string? Address,
    int SubcontractorsCount,
    bool IsFromZup);

public sealed record ProjectStaffOptionDto(
    Guid Id,
    string FullName,
    string Email,
    string? TansuRole);

public sealed record ProjectSubcontractorItemDto(
    Guid Id,
    string Name,
    string Bin,
    string ActivityType,
    int CompletionPercent,
    DateTimeOffset? ProgressReportedAt,
    string? ProgressReportedByFullName,
    int EmployeesCount,
    int ApprovedEmployeesCount);

public sealed record ProjectWorkforceItemDto(
    Guid EmployeeId,
    string FullName,
    string Position,
    string SubcontractorName,
    string? ApprovalStatus);

public sealed record ProjectTeamMemberDto(
    Guid UserId,
    string FullName,
    string Email,
    string? Position,
    string? TansuRole,
    string RoleLabel);

public sealed record ProjectDocumentDto(
    Guid Id,
    string Name,
    string DocumentType,
    string DocumentTypeLabel,
    string? ContentType,
    DateTimeOffset UploadedAt,
    string UploadedByFullName);

public sealed record ProjectDetailDto(
    Guid ProjectOid,
    int? ZupId,
    string? Code,
    string? Name,
    string? Description,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? ZupProjectManagerName,
    string? ContractType,
    DateTimeOffset? ZupSyncedAt,
    int SubcontractorsCount,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    decimal? BudgetAmount,
    string BudgetCurrency,
    Guid? ResponsibleAdminUserId,
    string? ResponsibleAdminFullName,
    string? ResponsibleAdminEmail,
    Guid? ProjectManagerUserId,
    string? ProjectManagerFullName,
    string? ProjectManagerEmail,
    IReadOnlyList<ProjectSubcontractorItemDto> Subcontractors,
    IReadOnlyList<ProjectWorkforceItemDto> Workforce,
    IReadOnlyList<ProjectTeamMemberDto> Team,
    IReadOnlyList<ProjectDocumentDto> Documents);

public sealed record UpdateProjectRequest(
    string? Name,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    decimal? BudgetAmount,
    string? BudgetCurrency,
    Guid? ResponsibleAdminUserId,
    Guid? ProjectManagerUserId);

public sealed record UploadProjectDocumentRequest(string Name, string DocumentType);
