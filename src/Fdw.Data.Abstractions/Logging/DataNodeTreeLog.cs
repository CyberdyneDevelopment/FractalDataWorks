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
    [MessageLogging(EventId = 11000, Level = LogLevel.Error,
        Message = "[DataNodeTree] Root node '{nodeName}' not found among the tree's root nodes")]
    public static partial IGenericMessage RootNodeNotFound(ILogger logger, string nodeName);

    /// <summary>
    /// Logs (at Debug) when a field with the requested name is not found among the fields of a data container.
    /// </summary>
    /// <param name="logger">The logger instance to write the structured log entry to.</param>
    /// <param name="fieldName">The name of the field that was not found.</param>
    /// <param name="containerName">The name of the container that was searched.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the field-not-found-in-container log event.</returns>
    [MessageLogging(EventId = 11001, Level = LogLevel.Warning,
        Message = "[DataNode] Field '{fieldName}' not found in container '{containerName}'")]
    public static partial IGenericMessage FieldNotFoundInContainer(ILogger logger, string fieldName, string containerName);

    /// <summary>
    /// Logs (at Debug) when navigation attempts to find a child node on a leaf field, which has no children.
    /// </summary>
    /// <param name="logger">The logger instance to write the structured log entry to.</param>
    /// <param name="fieldName">The name of the leaf field on which child navigation was attempted.</param>
    /// <param name="childName">The name of the child node that was requested.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the leaf-field-has-no-child log event.</returns>
    [MessageLogging(EventId = 11002, Level = LogLevel.Debug,
        Message = "[DataNode] Field '{fieldName}' is a leaf node and has no child node '{childName}'")]
    public static partial IGenericMessage LeafFieldHasNoChild(ILogger logger, string fieldName, string childName);
}
