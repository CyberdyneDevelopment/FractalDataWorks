using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Fdw.Web.RestEndpoints.Logging;

/// <summary>
/// MessageLogging shared by every REST endpoint area: the generic CRUD / connection-test /
/// validation events that are not specific to any one domain.
/// <para>
/// Why this exists: these seventeen methods were declared identically in three separate
/// ApiEndpointLog classes (Operations, Schema, Search) with the same EventIds. Search's copy
/// consisted of nothing else. Area-specific events stay in their own per-area log class.
/// </para>
/// <para>
/// Two events differed between the copies -- Schema had DomainDisabled at 41001 and
/// WriterUnavailable at 61000, where Operations and Search had 61000 and 61001. The
/// two-of-three values are used here; Schema's 61000 for WriterUnavailable additionally
/// collided with Operations' DomainDisabled, which is what the drift looked like in practice.
/// </para>
/// </summary>
[MessageLoggingTypeCode("ENDPOINT")]
public static partial class EndpointLog
{
    /// <summary>Logs the result of a connection test (succeeded or failed).</summary>
    [MessageLogging(
        EventId = 11010,
        Level = LogLevel.Information,
        Message = "Connection test for '{name}' {result}")]
    public static partial IGenericMessage ConnectionTestResult(
        ILogger logger,
        string name,
        string result);

    /// <summary>Logs that a resource was successfully created.</summary>
    [MessageLogging(
        EventId = 11004,
        Level = LogLevel.Information,
        Message = "Created {resourceName} '{name}'")]
    public static partial IGenericMessage CreatedResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs that a create operation is starting for a new resource.</summary>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "Creating {resourceName} '{name}'")]
    public static partial IGenericMessage CreatingResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs that a resource was successfully deleted.</summary>
    [MessageLogging(
        EventId = 11008,
        Level = LogLevel.Information,
        Message = "Deleted {resourceName} '{name}'")]
    public static partial IGenericMessage DeletedResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs that a delete operation is starting for a resource.</summary>
    [MessageLogging(
        EventId = 11007,
        Level = LogLevel.Information,
        Message = "Deleting {resourceName} '{name}'")]
    public static partial IGenericMessage DeletingResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs a warning that a domain is disabled via configuration.</summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "Domain '{domainName}' is disabled via configuration")]
    public static partial IGenericMessage DomainDisabled(
        ILogger logger,
        string domainName);

    /// <summary>Logs that a get operation is starting for a specific resource.</summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Trace,
        Message = "Getting {resourceName} '{name}'")]
    public static partial IGenericMessage GettingResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs the number of resources returned from a list operation.</summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "Listed {count} {resourceName}")]
    public static partial IGenericMessage ListedResources(
        ILogger logger,
        int count,
        string resourceName);

    /// <summary>Logs that a list operation is starting for the specified resource type.</summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "Listing {resourceName}")]
    public static partial IGenericMessage ListingResources(
        ILogger logger,
        string resourceName);

    /// <summary>Logs an error when a CRUD operation fails with an exception.</summary>
    [MessageLogging(
        EventId = 91000,
        Level = LogLevel.Error,
        Message = "Failed to {operation} {resourceName} '{name}'")]
    public static partial IGenericMessage OperationFailed(
        ILogger logger,
        Exception exception,
        string operation,
        string resourceName,
        string name);

    /// <summary>Logs a warning that the resource already exists during a create operation.</summary>
    [MessageLogging(
        EventId = 41000,
        Level = LogLevel.Warning,
        Message = "{resourceName} '{name}' already exists")]
    public static partial IGenericMessage ResourceAlreadyExists(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs a warning that the requested resource was not found.</summary>
    [MessageLogging(
        EventId = 31000,
        Level = LogLevel.Warning,
        Message = "{resourceName} '{name}' not found")]
    public static partial IGenericMessage ResourceNotFound(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs that a connection test is starting.</summary>
    [MessageLogging(
        EventId = 11009,
        Level = LogLevel.Information,
        Message = "Testing connection '{name}'")]
    public static partial IGenericMessage TestingConnection(
        ILogger logger,
        string name);

    /// <summary>Logs that a resource was successfully updated.</summary>
    [MessageLogging(
        EventId = 11006,
        Level = LogLevel.Information,
        Message = "Updated {resourceName} '{name}'")]
    public static partial IGenericMessage UpdatedResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs that an update operation is starting for an existing resource.</summary>
    [MessageLogging(
        EventId = 11005,
        Level = LogLevel.Trace,
        Message = "Updating {resourceName} '{name}'")]
    public static partial IGenericMessage UpdatingResource(
        ILogger logger,
        string resourceName,
        string name);

    /// <summary>Logs a warning when request validation fails for a resource operation.</summary>
    [MessageLogging(
        EventId = 21000,
        Level = LogLevel.Warning,
        Message = "Validation failed for {resourceName} '{name}': {reason}")]
    public static partial IGenericMessage ValidationFailed(
        ILogger logger,
        string resourceName,
        string name,
        string reason);

    /// <summary>Logs an error when the configuration writer is unavailable for a resource type.</summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "Configuration writer unavailable for {resourceName}")]
    public static partial IGenericMessage WriterUnavailable(
        ILogger logger,
        string resourceName);
}
