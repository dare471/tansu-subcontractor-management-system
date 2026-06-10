namespace Tansu.Application.Common.Interfaces;

public sealed record AuditEntry(
    string Action,
    string EntityType,
    Guid EntityId,
    string Summary,
    string? PayloadJson = null,
    Guid? ProjectOid = null,
    Guid? SubcontractorId = null,
    Guid? ActorUserId = null,
    string? ActorEmail = null,
    string? ActorType = null);

public interface IAuditRecorder
{
    void Record(AuditEntry entry);
}
