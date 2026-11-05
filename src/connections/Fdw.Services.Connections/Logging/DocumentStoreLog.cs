using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for finding and narrowing an organization's document store (FDW-1581, FDW-3396).
/// EventId ranges: 12800-12802 (store found), 22800-22801 (invalid input, 400), 32800-32802 (nothing assigned or granted, 404), 42800-42801 (a command or
/// result that cannot stay inside the folder, 409), 72800-72801 (store failures, 500), 102800 (another organization's assignment, 403).
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class DocumentStoreLog
{
    /// <summary>Logs that an organization's store was found.</summary>
    [MessageLogging(EventId = 12800, Level = LogLevel.Information, Message = "Organization '{organizationId}' uses connection '{connectionName}' for '{purpose}', narrowed to folder '{root}'")]
    public static partial IGenericMessage StoreFound(ILogger logger, Guid organizationId, string connectionName, string purpose, string root);

    /// <summary>Logs that a store was reopened through the assignment a file was stored under (FDW-3396).</summary>
    [MessageLogging(EventId = 12801, Level = LogLevel.Information, Message = "Organization '{organizationId}' reads through assignment '{storedUnder}': connection '{connectionName}' for '{purpose}', narrowed to folder '{root}'")]
    public static partial IGenericMessage StoredUnderFound(ILogger logger, Guid organizationId, Guid storedUnder, string connectionName, string purpose, string root);

    /// <summary>Logs that an organization's document records were found on its DocumentRecord connection (FDW-3673).</summary>
    [MessageLogging(EventId = 12802, Level = LogLevel.Information, Message = "Organization '{organizationId}' keeps its document records on connection '{connectionName}'")]
    public static partial IGenericMessage RecordStoreFound(ILogger logger, Guid organizationId, string connectionName);

    /// <summary>Logs that the purpose was not stated.</summary>
    [MessageLogging(EventId = 22800, Level = LogLevel.Warning, Message = "A document store was asked for organization '{organizationId}' without a purpose")]
    public static partial IGenericMessage PurposeRequired(ILogger logger, Guid organizationId);

    /// <summary>Logs that a folder of a grant or an assignment is not a valid path.</summary>
    [MessageLogging(EventId = 22801, Level = LogLevel.Warning, Message = "The {what} folder '{folder}' of organization '{organizationId}' is not a valid folder: {reason}")]
    public static partial IGenericMessage FolderInvalid(ILogger logger, string what, string? folder, Guid organizationId, string? reason);

    /// <summary>Logs that the organization has no assignment for the purpose.</summary>
    [MessageLogging(EventId = 32800, Level = LogLevel.Warning, Message = "Organization '{organizationId}' has no connection assigned for '{purpose}'",
        ClientMessage = "The organization has no connection assigned for '{purpose}'. Assign one to the organization first")]
    public static partial IGenericMessage NothingAssigned(ILogger logger, Guid organizationId, string purpose);

    /// <summary>Logs that an assignment names a grant that does not exist.</summary>
    [MessageLogging(EventId = 32801, Level = LogLevel.Error, Message = "Organization '{organizationId}' has a connection assigned for '{purpose}' but the grant '{grantId}' it names does not exist")]
    public static partial IGenericMessage GrantMissing(ILogger logger, Guid organizationId, string purpose, Guid grantId);

    /// <summary>Logs that the assignment a file was stored under does not exist (FDW-3396).</summary>
    [MessageLogging(EventId = 32802, Level = LogLevel.Warning, Message = "Organization '{organizationId}' asked for a store through assignment '{storedUnder}', which does not exist",
        ClientMessage = "The assignment '{storedUnder}' the file was stored under does not exist")]
    public static partial IGenericMessage StoredUnderNotFound(ILogger logger, Guid organizationId, Guid storedUnder);

    /// <summary>Logs that the assignment a file was stored under belongs to another organization (FDW-3396).</summary>
    [MessageLogging(EventId = 102800, Level = LogLevel.Warning, Message = "Organization '{organizationId}' was refused a store through assignment '{storedUnder}': it belongs to organization '{owner}'",
        ClientMessage = "The assignment '{storedUnder}' belongs to another organization")]
    public static partial IGenericMessage StoredUnderOtherOrganization(ILogger logger, Guid organizationId, Guid storedUnder, Guid owner);

    /// <summary>Logs that the connection an organization was assigned for its document records does not fit that purpose (FDW-3673).</summary>
    [MessageLogging(EventId = 42801, Level = LogLevel.Warning, Message = "Organization '{organizationId}' was assigned connection '{connectionName}' for '{purpose}', which does not fit it: {reason}",
        ClientMessage = "The connection assigned for '{purpose}' cannot keep document records")]
    public static partial IGenericMessage RecordConnectionUnfit(ILogger logger, Guid organizationId, string connectionName, string purpose, string reason);

    /// <summary>Logs that a command cannot be narrowed to the folder.</summary>
    [MessageLogging(EventId = 42800, Level = LogLevel.Warning, Message = "A '{commandType}' command was refused: it cannot be kept inside the folder '{root}'")]
    public static partial IGenericMessage CommandNotNarrowable(ILogger logger, string commandType, string root);

    /// <summary>Logs that an assignment or grant could not be read.</summary>
    [MessageLogging(EventId = 72800, Level = LogLevel.Error, Message = "{what} could not be read: {reason}")]
    public static partial IGenericMessage ReadFailed(ILogger logger, string what, string? reason);

    /// <summary>Logs that the granted connection could not be opened.</summary>
    [MessageLogging(EventId = 72801, Level = LogLevel.Error, Message = "Connection '{connectionName}' granted for '{purpose}' could not be opened: {reason}")]
    public static partial IGenericMessage ConnectionNotOpened(ILogger logger, string connectionName, string purpose, string? reason);
}
