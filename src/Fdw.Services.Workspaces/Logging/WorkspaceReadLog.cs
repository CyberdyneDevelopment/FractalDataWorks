using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for <see cref="WorkspaceReadService"/>: what a caller may read through workspaces.
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class WorkspaceReadLog
{
    /// <summary>Logs that a data set is being resolved through a workspace.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace asked through.</param>
    /// <param name="dataSetName">The name the data set was asked for by.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11060, Level = LogLevel.Trace,
        Message = "Resolving data set '{dataSetName}' through workspace '{workspaceName}'")]
    public static partial IGenericMessage ResolvingDataSet(ILogger logger, string workspaceName, string dataSetName);

    /// <summary>Logs that a data set was resolved through a workspace.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace asked through.</param>
    /// <param name="dataSetName">The name the data set was asked for by.</param>
    /// <param name="executionTarget">The data set its query runs.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11061, Level = LogLevel.Debug,
        Message = "Data set '{dataSetName}' in workspace '{workspaceName}' runs as data set '{executionTarget}'")]
    public static partial IGenericMessage DataSetResolved(ILogger logger, string workspaceName, string dataSetName, string executionTarget);

    /// <summary>Logs that the data sets attached to a workspace are being listed.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace whose attachments are listed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11062, Level = LogLevel.Trace,
        Message = "Listing the data sets attached to workspace '{workspaceName}'")]
    public static partial IGenericMessage ListingDataSets(ILogger logger, string workspaceName);

    /// <summary>Logs that an attached data set could not be loaded and was left out of a listing.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace being listed.</param>
    /// <param name="attachmentName">The attachment that could not be loaded.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    /// <remarks>
    /// Why Warning: the listing still answers, but a data set the owner attached is missing from it until
    /// its own failure, logged where it was read, is fixed.
    /// </remarks>
    [MessageLogging(EventId = 11063, Level = LogLevel.Warning,
        Message = "Data set attachment '{attachmentName}' of workspace '{workspaceName}' could not be loaded and is left out of the listing")]
    public static partial IGenericMessage AttachmentLeftOut(ILogger logger, string workspaceName, string attachmentName);
}
