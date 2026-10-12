using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.ApiReference.Logging;

/// <summary>
/// MessageLogging for the API reference: generating the OpenAPI document, cutting it to one caller,
/// and listing the reference pages a caller may open.
/// </summary>
[MessageLoggingTypeCode("API")]
public static partial class ApiReferenceLog
{
    /// <summary>Logs that the OpenAPI document was generated and kept for every later request.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="documentName">The document's registration name.</param>
    /// <param name="length">The generated document's length in characters.</param>
    // Information: this happens once per process, and every API reference page depends on it.
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "Generated OpenAPI document '{documentName}' ({length} characters); every API reference is cut from it")]
    public static partial IGenericMessage DocumentGenerated(ILogger logger, string documentName, int length);

    /// <summary>Logs that a request found the document already generated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="documentName">The document's registration name.</param>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Trace,
        Message = "OpenAPI document '{documentName}' already generated; cutting a copy for this request")]
    public static partial IGenericMessage DocumentReused(ILogger logger, string documentName);

    /// <summary>Logs how the document was cut for one caller.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="caller">Who the caller is: anonymous, signed in, or platform admin.</param>
    /// <param name="kept">The operations left in the document.</param>
    /// <param name="removed">The operations taken out.</param>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Debug,
        Message = "API reference cut for a {caller} caller: kept {kept} operations, removed {removed}")]
    public static partial IGenericMessage OperationsFiltered(ILogger logger, string caller, int kept, int removed);

    /// <summary>Traces one operation taken out of a caller's document.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="method">The operation's HTTP method.</param>
    /// <param name="path">The operation's path.</param>
    /// <param name="reason">Why the caller may not see it.</param>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Trace,
        Message = "API reference removed {method} {path}: {reason}")]
    public static partial IGenericMessage OperationRemoved(ILogger logger, string method, string path, string reason);

    /// <summary>Logs that paths outside a reference's scope were taken out.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="removed">The paths taken out.</param>
    /// <param name="kept">The paths left.</param>
    [MessageLogging(
        EventId = 11030,
        Level = LogLevel.Debug,
        Message = "API reference scoped to its paths: kept {kept}, removed {removed}")]
    public static partial IGenericMessage PathsScoped(ILogger logger, int removed, int kept);

    /// <summary>Logs that schemas no remaining operation uses were taken out.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="removed">The schemas taken out.</param>
    /// <param name="kept">The schemas left.</param>
    [MessageLogging(
        EventId = 11031,
        Level = LogLevel.Debug,
        Message = "API reference pruned {removed} unused schemas; {kept} remain")]
    public static partial IGenericMessage SchemasPruned(ILogger logger, int removed, int kept);

    /// <summary>Logs a reference document sent to the caller.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="endpoint">The document endpoint that served it.</param>
    /// <param name="operations">The operations in the document.</param>
    /// <param name="length">The document's length in characters.</param>
    [MessageLogging(
        EventId = 11032,
        Level = LogLevel.Debug,
        Message = "API reference {endpoint} served {operations} operations ({length} characters)")]
    public static partial IGenericMessage DocumentServed(ILogger logger, string endpoint, int operations, int length);

    /// <summary>Logs the reference pages offered to a caller.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The links offered.</param>
    /// <param name="pages">The registered pages considered.</param>
    [MessageLogging(
        EventId = 11033,
        Level = LogLevel.Debug,
        Message = "Offered {count} API reference links from {pages} registered pages")]
    public static partial IGenericMessage PagesListed(ILogger logger, int count, int pages);

    /// <summary>Logs an API reference page mapped onto the host.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="route">Where the page is served.</param>
    /// <param name="title">The page's title.</param>
    /// <param name="access">Who may open it.</param>
    [MessageLogging(
        EventId = 11035,
        Level = LogLevel.Debug,
        Message = "API reference page '{title}' mapped at {route}; {access}")]
    public static partial IGenericMessage PageMapped(ILogger logger, string route, string title, string access);

    /// <summary>Logs the data set operations a workspace contributed to a reference.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspace">The workspace.</param>
    /// <param name="dataSets">The data set operations added for it.</param>
    [MessageLogging(
        EventId = 11034,
        Level = LogLevel.Debug,
        Message = "Workspace '{workspace}' contributed {dataSets} data set operations to the API reference")]
    public static partial IGenericMessage WorkspaceDescribed(ILogger logger, string workspace, int dataSets);

    /// <summary>Logs an attached data set left out of a reference because it could not be loaded.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="workspace">The workspace the data set is attached to.</param>
    /// <param name="dataSetId">The attached data set's id.</param>
    /// <param name="reason">Why it was left out.</param>
    // Warning: the reference is still served, one data set short, as the data route still serves the others.
    [MessageLogging(
        EventId = 41001,
        Level = LogLevel.Warning,
        Message = "Data set {dataSetId} attached to workspace '{workspace}' was left out of the API reference: {reason}")]
    public static partial IGenericMessage DataSetLeftOut(ILogger logger, string workspace, Guid dataSetId, string reason);

    /// <summary>Logs that the OpenAPI document could not be generated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">What the generator threw.</param>
    /// <param name="documentName">The document's registration name.</param>
    [MessageLogging(
        EventId = 91016,
        Level = LogLevel.Error,
        Message = "OpenAPI document '{documentName}' could not be generated")]
    public static partial IGenericMessage DocumentGenerationFailed(ILogger logger, Exception exception, string documentName);
}
