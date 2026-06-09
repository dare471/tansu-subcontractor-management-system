using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tansu.Application.Common.Interfaces;
using Tansu.Domain.Entities;
using Tansu.Domain.Enums;

namespace Tansu.Infrastructure.Audit;

public sealed class DbAuditRecorder(
    ITansuDbContext db,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IAuditRecorder
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public void Record(AuditEntry entry)
    {
        var http = httpContextAccessor.HttpContext;
        var actorType = entry.ActorType ?? ResolveActorType(currentUser.UserType);
        var actorUserId = entry.ActorUserId ?? currentUser.UserId;
        var actorEmail = entry.ActorEmail ?? currentUser.Email;

        db.AuditEvents.Add(new AuditEvent
        {
            OccurredAt = DateTimeOffset.UtcNow,
            ActorUserId = actorUserId,
            ActorEmail = actorEmail,
            ActorType = actorType,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            ProjectOid = entry.ProjectOid,
            SubcontractorId = entry.SubcontractorId,
            Summary = entry.Summary,
            PayloadJson = entry.PayloadJson,
            CorrelationId = http?.TraceIdentifier,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers.TryGetValue("User-Agent", out var ua) == true
                ? ua.ToString()
                : null
        });
    }

    private static string ResolveActorType(string? userType) => userType switch
    {
        UserType.Tansu => AuditActorType.Tansu,
        UserType.Subcontractor => AuditActorType.Subcontractor,
        UserType.Employee => AuditActorType.Employee,
        _ => AuditActorType.System
    };
}
