using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions.Logging;

/// <summary>
/// MessageLogging for <see cref="DataNodeTree{TRoot}"/> root-node navigation.
/// EventId range: 5932-5945.
/// </summary>
[MessageLoggingTypeCode("MAPPER")]
public static partial class DataNodeTreeLog
{
    /// <summary>
    /// Logs a warning when a root node with the requested name is not found among the tree's root nodes.
    /// </summary>
    /// <param name="logger">The logger instance to write the structured log entry to.</param>
    /// <param name="nodeName">The name of the root node that was not found.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the root-node-not-found log event.</returns>
    // Why Error, not Debug (FDW-583): a root-node lookup by name is a terminal, addressed lookup (not
    // a probe loop over many candidates) — a miss is the final answer and the operation cannot
    // complete, so it must print instead of sitting below the default log threshold.
    [MessageLogging(EventId = 11000, Level = LogLevel.Error,
        Message = "[DataNodeTree] Root node '{nodeName}' not found among the tree's root nodes")]
    public static partial IGenericMessage RootNodeNotFound(ILogger logger, string nodeName);

    // Why: container field-child navigation (IDataNode.Node on a DataContainer) — the field set is
    // the container's child nodes. Lives here (Data.Abstractions) with the DataContainer base so the
    // base is reachable by every upstream transport package without a Services.Data back-reference.
    /// <summary>
    /// Logs (at Debug) when a field with the requested name is not found among the fields of a data container.
    /// </summary>
    /// <param name="logger">The logger instance to write the structured log entry to.</param>
    /// <param name="fieldName">The name of the field that was not found.</param>
    /// <param name="containerName">The name of the container that was searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the field-not-found-in-container log event.</returns>
    // Why Warning, not Debug (FDW-583): still a routine caller-handled navigation miss (returns
    // IGenericResult.Failure as control flow), but a field absent from a container's declared schema
    // is an abnormal-but-handled condition worth surfacing above the default print threshold.
    [MessageLogging(EventId = 11001, Level = LogLevel.Warning,
        Message = "[DataNode] Field '{fieldName}' not found in container '{containerName}'")]
    public static partial IGenericMessage FieldNotFoundInContainer(ILogger logger, string fieldName, string containerName);

    // Why: IDataField is a leaf IDataNode — Node(name) always fails because a field has no children.
    // Lives here so leaf fields in every transport package (MsSqlDataField, PostgreSqlDataField) and
    // the generic DataField can fail loud without a Services.Data back-reference.
    /// <summary>
    /// Logs (at Debug) when navigation attempts to find a child node on a leaf field, which has no children.
    /// </summary>
    /// <param name="logger">The logger instance to write the structured log entry to.</param>
    /// <param name="fieldName">The name of the leaf field on which child navigation was attempted.</param>
    /// <param name="childName">The name of the child node that was requested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the leaf-field-has-no-child log event.</returns>
    // Why: a leaf-field child lookup is routine caller-handled control flow (returns Failure) — Debug, not Warning.
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "[DataNode] Field '{fieldName}' is a leaf node and has no child node '{childName}'")]
    public static partial IGenericMessage LeafFieldHasNoChild(ILogger logger, string fieldName, string childName);
}
