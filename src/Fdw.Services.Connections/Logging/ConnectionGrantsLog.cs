using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for the connections granted to tenants and assigned to organizations (FDW-1581).
/// EventId ranges: 12810-12813 (done), 22810-22811 (invalid input, 400), 32810-32814 (not found, 404), 42810-42812 (conflict, 409),
/// 72810-72811 (store failures, 500), 102810 (a grant of another tenant, 403).
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class ConnectionGrantsLog
{
    /// <summary>Logs that a connection was granted.</summary>
    [MessageLogging(EventId = 12810, Level = LogLevel.Information, Message = "Connection '{connectionName}' was granted to tenant '{tenantId}' for '{purpose}' (folder '{folder}')")]
    public static partial IGenericMessage Granted(ILogger logger, string connectionName, Guid tenantId, string purpose, string folder);

    /// <summary>Logs that a grant was taken back.</summary>
    [MessageLogging(EventId = 12811, Level = LogLevel.Information, Message = "Connection '{connectionName}' was taken back from tenant '{tenantId}' for '{purpose}'")]
    public static partial IGenericMessage Revoked(ILogger logger, string connectionName, Guid tenantId, string purpose);

    /// <summary>Logs that a connection was assigned to an organization.</summary>
    [MessageLogging(EventId = 12812, Level = LogLevel.Information, Message = "Grant '{grantId}' was assigned to organization '{organizationId}' for '{purpose}' (base folder '{baseFolder}')")]
    public static partial IGenericMessage Assigned(ILogger logger, Guid grantId, Guid organizationId, string purpose, string baseFolder);

    /// <summary>Logs that an assignment was removed.</summary>
    [MessageLogging(EventId = 12813, Level = LogLevel.Information, Message = "The assignment of '{purpose}' to organization '{organizationId}' was removed")]
    public static partial IGenericMessage Unassigned(ILogger logger, string purpose, Guid organizationId);

    /// <summary>Logs that a required text was not given.</summary>
    [MessageLogging(EventId = 22810, Level = LogLevel.Warning, Message = "A connection grant or assignment was refused: it has no {field}")]
    public static partial IGenericMessage FieldRequired(ILogger logger, string field);

    /// <summary>Logs that a folder is not a valid folder.</summary>
    [MessageLogging(EventId = 22811, Level = LogLevel.Warning, Message = "The {what} folder '{folder}' was refused: {reason}")]
    public static partial IGenericMessage FolderInvalid(ILogger logger, string what, string? folder, string? reason);

    /// <summary>Logs that the tenant does not exist.</summary>
    [MessageLogging(EventId = 32810, Level = LogLevel.Warning, Message = "Tenant '{tenantId}' does not exist")]
    public static partial IGenericMessage TenantNotFound(ILogger logger, Guid tenantId);

    /// <summary>Logs that the connection is not a document connection the platform has.</summary>
    [MessageLogging(EventId = 32811, Level = LogLevel.Warning, Message = "Connection '{connectionName}' is not a document connection: {reason}")]
    public static partial IGenericMessage NotADocumentConnection(ILogger logger, string connectionName, string? reason);

    /// <summary>Logs that the grant does not exist.</summary>
    [MessageLogging(EventId = 32812, Level = LogLevel.Warning, Message = "There is no grant of connection '{connectionName}' to tenant '{tenantId}' for '{purpose}'")]
    public static partial IGenericMessage GrantNotFound(ILogger logger, string connectionName, Guid tenantId, string purpose);

    /// <summary>Logs that a grant named by its id does not exist.</summary>
    [MessageLogging(EventId = 32813, Level = LogLevel.Warning, Message = "There is no grant '{grantId}'")]
    public static partial IGenericMessage GrantIdNotFound(ILogger logger, Guid grantId);

    /// <summary>Logs that the organization has no assignment for the purpose.</summary>
    [MessageLogging(EventId = 32814, Level = LogLevel.Warning, Message = "Organization '{organizationId}' has no connection assigned for '{purpose}'")]
    public static partial IGenericMessage AssignmentNotFound(ILogger logger, Guid organizationId, string purpose);

    /// <summary>Logs that the tenant already holds the grant.</summary>
    [MessageLogging(EventId = 42810, Level = LogLevel.Warning, Message = "Tenant '{tenantId}' already holds connection '{connectionName}' for '{purpose}'")]
    public static partial IGenericMessage GrantExists(ILogger logger, Guid tenantId, string connectionName, string purpose);

    /// <summary>Logs that a grant was kept because organizations are assigned to it.</summary>
    [MessageLogging(EventId = 42811, Level = LogLevel.Warning, Message = "The grant of '{connectionName}' to tenant '{tenantId}' was kept: {organizations} organizations are assigned to it")]
    public static partial IGenericMessage GrantAssigned(ILogger logger, string connectionName, Guid tenantId, int organizations);

    /// <summary>Logs that the grant is for another purpose than the assignment.</summary>
    [MessageLogging(EventId = 42812, Level = LogLevel.Warning, Message = "Grant '{grantId}' is for '{grantPurpose}', not '{purpose}'")]
    public static partial IGenericMessage PurposeDiffers(ILogger logger, Guid grantId, string grantPurpose, string purpose);

    /// <summary>Logs that a read from the store failed.</summary>
    [MessageLogging(EventId = 72810, Level = LogLevel.Error, Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that a write to the store failed.</summary>
    [MessageLogging(EventId = 72811, Level = LogLevel.Error, Message = "{what} could not be written: {reason}")]
    public static partial IGenericMessage WriteFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that the grant belongs to a tenant the organization is not in.</summary>
    [MessageLogging(EventId = 102810, Level = LogLevel.Warning, Message = "Grant '{grantId}' was refused for organization '{organizationId}': it was granted to another tenant")]
    public static partial IGenericMessage NotTheTenantsGrant(ILogger logger, Guid grantId, Guid organizationId);
}
