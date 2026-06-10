namespace Tansu.Application.Common.Interfaces;

public sealed record AccessControlPerson(
    Guid EmployeeId,
    string FullName,
    byte[]? PhotoBytes,
    string? CardNumber,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);

public sealed record AccessControlEvent(
    Guid EmployeeId,
    DateTimeOffset OccurredAt,
    string VendorId,
    string? TerminalLocation);

public interface IAccessControlSystem
{
    string VendorId { get; }
    Task SyncPersonAsync(AccessControlPerson person, CancellationToken ct);
    Task RevokePersonAsync(Guid employeeId, string reason, CancellationToken ct);
    Task<bool> IsHealthyAsync(CancellationToken ct);
}

public interface IAccessControlOrchestrator
{
    Task SyncPersonAsync(AccessControlPerson person, Guid? projectOid, CancellationToken ct);
    Task RevokePersonAsync(Guid employeeId, string reason, Guid? projectOid, CancellationToken ct);
}
