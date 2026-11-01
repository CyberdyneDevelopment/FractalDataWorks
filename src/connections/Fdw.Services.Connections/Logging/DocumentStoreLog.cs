using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Logging;

/// <summary>
/// MessageLogging for finding and narrowing an organization's document store (FDW-1581).
/// EventId ranges: 12800 (store found), 22800-22801 (invalid input, 400), 32800-32801 (nothing assigned or granted, 404), 42800 (a command or
/// result that cannot stay inside the folder, 409), 72800-72801 (store failures, 500).
/// </summary>
[MessageLoggingTypeCode("CONNECTIONS")]
public static partial class DocumentStoreLog
{
    /// <summary>Logs that an organization's store was found.</summary>
    [MessageLogging(EventId = 12800, Level = LogLevel.Information, Message = "Organization '{organizationId}' uses connection '{connectionName}' for '{purpose}', narrowed to folder '{root}'")]
    public static partial IGenericMessage StoreFound(ILogger logger, Guid organizationId, string connectionName, string purpose, string root);

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
