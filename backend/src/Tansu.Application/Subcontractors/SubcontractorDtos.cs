namespace Tansu.Application.Subcontractors;

public sealed record SubcontractorDto(
    Guid Id,
    string Name,
    string Bin,
    int ProjectsCount,
    int EmployeesApprovedCount,
    int EmployeesNotApprovedCount,
    bool IsActive,
    Guid? ManagerUserId,
    string? ManagerFullName,
    DateTimeOffset CreatedAt);

public sealed record CreateSubcontractorRequest(
    string Name,
    string Bin,
    Guid? ProjectOid = null,
    string? ProjectName = null,
    string? ActivityType = null);
public sealed record UpdateSubcontractorRequest(string Name, string Bin, Guid? ManagerUserId = null);
public sealed record SubcontractorDocumentDto(
    Guid Id,
    string Name,
    string DocumentType,
    string DocumentTypeLabel,
    string? ContentType,
    DateTimeOffset UploadedAt,
    string UploadedByFullName);
public sealed record BindProjectRequest(Guid ProjectOid, string? ProjectName, string ActivityType);
public sealed record BindProjectFromProjectRequest(Guid SubcontractorId, string ActivityType);
public sealed record UpdateProjectSubcontractorBindingRequest(string ActivityType);
public sealed record ReportProjectProgressRequest(int CompletionPercent);
