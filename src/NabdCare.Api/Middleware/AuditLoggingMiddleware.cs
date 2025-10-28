using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NabdCare.Application.Common;
using NabdCare.Domain.Entities.Audits;
using NabdCare.Infrastructure.Persistence;

namespace NabdCare.Api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        NabdCareDbContext db,
        ITenantContext tenant,
        IUserContext userContext)
    {
        var previousStates = CaptureState(db);

        await _next(context);

        await SaveAuditLogsAsync(context, db, tenant, userContext, previousStates);
    }

    private static Dictionary<EntityEntry, Dictionary<string, object?>> CaptureState(NabdCareDbContext db)
    {
        return db.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified)
            .ToDictionary(
                e => e,
                e => e.OriginalValues.Properties.ToDictionary(
                    p => p.Name,
                    p => e.OriginalValues[p]
                )
            );
    }

    private async Task SaveAuditLogsAsync(
        HttpContext context,
        NabdCareDbContext db,
        ITenantContext tenant,
        IUserContext userContext,
        Dictionary<EntityEntry, Dictionary<string, object?>> previousStates)
    {
        // ✅ FIX: Skip entirely if authentication failed (Swagger Bearer-only case)
        if (!(context.User?.Identity?.IsAuthenticated ?? false))
        {
            return;
        }

        if (db.ChangeTracker.Entries().All(e => e.State == EntityState.Unchanged))
            return;

        var ip = context.Connection.RemoteIpAddress?.ToString();
        var ua = context.Request.Headers["User-Agent"].ToString() ?? "Unknown";

        var userId = tenant.UserId;
        var userEmail = tenant.UserEmail;
        var clinicId = tenant.ClinicId;

        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Unchanged)
                continue;

            try
            {
                var audit = new AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    EntityType = entry.Entity.GetType().Name,
                    EntityId = GetPrimaryKey(entry),
                    Action = entry.State.ToString(),
                    UserId = userId,
                    UserEmail = userEmail ?? "Unknown",
                    ClinicId = clinicId,
                    IpAddress = ip,
                    UserAgent = ua,
                    Changes = entry.State == EntityState.Modified
                        ? GetChangesJson(entry, previousStates)
                        : null
                };

                db.AuditLogs.Add(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to log audit for entity {Entity}",
                    entry.Entity.GetType().Name);
            }
        }

        await db.SaveChangesAsync();
    }

    private static Guid? GetPrimaryKey(EntityEntry entry)
    {
        var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        return pk != null ? pk.CurrentValue as Guid? : null;
    }

    private static string? GetChangesJson(
        EntityEntry entry,
        Dictionary<EntityEntry, Dictionary<string, object?>> previousStates)
    {
        if (!previousStates.ContainsKey(entry)) return null;

        var original = previousStates[entry];
        var changes = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            var oldValue = original[prop.Metadata.Name];
            var newValue = prop.CurrentValue;

            if (!Equals(oldValue, newValue))
            {
                changes[prop.Metadata.Name] = new
                {
                    Old = oldValue,
                    New = newValue
                };
            }
        }

        return changes.Any()
            ? JsonSerializer.Serialize(changes)
            : null;
    }
}