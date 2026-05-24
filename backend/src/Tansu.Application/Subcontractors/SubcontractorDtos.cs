namespace Tansu.Application.Subcontractors;

public sealed record SubcontractorDto(
    Guid Id,
    string Name,
    string Bin,
    int ProjectsCount,
    int UsersCount,
    DateTimeOffset CreatedAt);

public sealed record CreateSubcontractorRequest(string Name, string Bin);
public sealed record UpdateSubcontractorRequest(string Name, string Bin);
public sealed record BindProjectRequest(Guid ProjectOid, string? ProjectName);
