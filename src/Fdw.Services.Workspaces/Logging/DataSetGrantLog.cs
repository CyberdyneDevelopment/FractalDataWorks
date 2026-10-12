using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Logging;

/// <summary>
/// MessageLogging for <see cref="WorkspaceAccessPolicy.MayAttachDataSet"/>: deciding whether the
/// caller may grant a data set to a workspace (FDW-937).
/// </summary>
[MessageLoggingTypeCode("WORKSPACES")]
public static partial class DataSetGrantLog
{
    /// <summary>Logs that a data set grant is being decided.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace the data set would be attached to.</param>
    /// <param name="dataSetId">The data set's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11040, Level = LogLevel.Trace,
        Message = "Deciding whether the caller may attach data set {dataSetId} to workspace '{workspaceName}'")]
    public static partial IGenericMessage DecidingGrant(ILogger logger, string workspaceName, string dataSetId);

    /// <summary>Logs that the caller holds a grant for the data set.</summary>
    /// <param name="logger">The logger that records the event.</param>
    /// <param name="workspaceName">The workspace the data set will be attached to.</param>
    /// <param name="dataSetId">The data set's Id.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 11041, Level = LogLevel.Debug,
        Message = "The caller may attach data set {dataSetId} to workspace '{workspaceName}'")]
    public static partial IGenericMessage GrantHeld(ILogger logger, string workspaceName, string dataSetId);
}
