using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Services.Authorization.Abstractions.Logging;

/// <summary>
/// High-performance logger for authorization events.
/// </summary>
[MessageLoggingTypeCode("ABSTRACTIONS5")]
public static partial class AuthorizationLogger
{
    /// <summary>
    /// Logs when authorization is denied.
    /// </summary>
    [MessageLogging(EventId = 51000, Level = LogLevel.Warning,
        Message = "Authorization denied: user {userId} on {resource}:{action}")]
    public static partial IGenericMessage AuthorizationDenied(
        ILogger logger, string userId, string resource, string action);

    /// <summary>
    /// Logs when authorization is granted.
    /// </summary>
    [MessageLogging(EventId = 11000, Level = LogLevel.Debug,
        Message = "Authorization granted: user {userId} on {resource}:{action}")]
    public static partial IGenericMessage AuthorizationGranted(
        ILogger logger, string userId, string resource, string action);

    /// <summary>
    /// Logs when a user lacks required permissions.
    /// </summary>
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning,
        Message = "Insufficient permissions: user {userId} lacks {permission}")]
    public static partial IGenericMessage InsufficientPermissions(
        ILogger logger, string userId, string permission);

    /// <summary>
    /// Logs when a required role is missing.
    /// </summary>
    [MessageLogging(EventId = 51002, Level = LogLevel.Warning,
        Message = "Role required: user {userId} requires role {role}")]
    public static partial IGenericMessage RoleRequired(
        ILogger logger, string userId, string role);

    /// <summary>
    /// Logs when tenant access is denied.
    /// </summary>
    [MessageLogging(EventId = 51003, Level = LogLevel.Warning,
        Message = "Tenant access denied: user {userId} for tenant {tenantId}")]
    public static partial IGenericMessage TenantAccessDenied(
        ILogger logger, string userId, Guid tenantId);

    /// <summary>
    /// Logs when tenant context is established.
    /// </summary>
    [MessageLogging(EventId = 11001, Level = LogLevel.Debug,
        Message = "Tenant context set: {tenantId} ({tenantSlug})")]
    public static partial IGenericMessage TenantContextSet(
        ILogger logger, Guid tenantId, string tenantSlug);

    /// <summary>
    /// Logs a security audit event.
    /// </summary>
    [MessageLogging(EventId = 11002, Level = LogLevel.Information,
        Message = "SECURITY AUDIT: {eventType} by {userId} on {resource}")]
    public static partial IGenericMessage SecurityAudit(
        ILogger logger, string eventType, string userId, string resource);

    /// <summary>
    /// Logs a security audit event with tenant context.
    /// </summary>
    [MessageLogging(EventId = 11003, Level = LogLevel.Information,
        Message = "SECURITY AUDIT: {eventType} by {userId} on {resource} in tenant {tenantId}")]
    public static partial IGenericMessage SecurityAuditWithTenant(
        ILogger logger, string eventType, string userId, string resource, Guid tenantId);

    /// <summary>
    /// Logs when permission check starts.
    /// </summary>
    [MessageLogging(EventId = 11004, Level = LogLevel.Trace,
        Message = "Checking permission {permission} for user {userId}")]
    public static partial IGenericMessage CheckingPermission(
        ILogger logger, string permission, string userId);

    /// <summary>
    /// Logs when role check starts.
    /// </summary>
    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "Checking role {role} for user {userId}")]
    public static partial IGenericMessage CheckingRole(
        ILogger logger, string role, string userId);
}
