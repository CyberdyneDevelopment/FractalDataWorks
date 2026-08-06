using System;
using System.Collections.Generic;
using System.Globalization;
using Fdw.Results;
using Fdw.Services.Pipelines.Components.Logging;
using Fdw.UI.Abstractions.Canvas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// Pure, render-agnostic map from a palette drag-and-drop name to the
/// <see cref="ICanvasNodeType"/> and seed metadata a new canvas node should carry.
/// </summary>
/// <remarks>
/// <para>
/// Closes the mechanism gap where a palette drop handler (e.g. the designer's <c>OnCanvasDrop</c>)
/// resolved <c>ICanvasNodeType</c> by calling <c>CanvasNodeTypes.ByName(paletteName)</c> directly —
/// palette names like <c>"Source"</c>/<c>"Map"</c>/<c>"Filter"</c> are UI-facing labels, not
/// registered <see cref="CanvasNodeTypes"/> names, so every drop failed loud before reaching this
/// map existed.
/// </para>
/// <para>
/// Palette names and their resulting node kind + seed metadata:
/// <list type="bullet">
/// <item><c>"Source"</c> → <c>DataSet</c> node, <c>DataSetRole</c> = <c>Source</c>.</item>
/// <item><c>"Destination"</c> → <c>DataSet</c> node, <c>DataSetRole</c> = <c>Sink</c>.</item>
/// <item>
/// <c>"Map"</c>/<c>"Filter"</c>/<c>"Aggregate"</c>/<c>"Calculate"</c>/<c>"Lookup"</c> → <c>Transform</c>
/// node, <c>OperationType</c> = the palette name, <c>ExecutionOrder</c> = the caller-supplied next
/// execution order.
/// </item>
/// </list>
/// Any other name fails loud — an unregistered palette name is a configuration error, not something
/// to paper over with a placeholder node type.
/// </para>
/// </remarks>
public static class PipelinePaletteNodeMap
{
    /// <summary>
    /// Maps a palette drag-and-drop name to its <see cref="ICanvasNodeType"/> and seed metadata.
    /// </summary>
    /// <param name="paletteName">
    /// The palette entry name (<c>"Source"</c>, <c>"Destination"</c>, <c>"Map"</c>, <c>"Filter"</c>,
    /// <c>"Aggregate"</c>, <c>"Calculate"</c>, or <c>"Lookup"</c>).
    /// </param>
    /// <param name="nextExecutionOrder">
    /// The execution order to seed on a new Transform node's metadata (ignored for DataSet node
    /// kinds). Callers compute this from the current count of Transform nodes on the canvas.
    /// </param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger"/> when null.</param>
    /// <returns>
    /// A result carrying the resolved node type and its seed metadata dictionary on success, or the
    /// unknown-palette-name failure otherwise.
    /// </returns>
    public static IGenericResult<(ICanvasNodeType NodeType, IReadOnlyDictionary<string, string> Metadata)> Map(
        string paletteName,
        int nextExecutionOrder,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;

        switch (paletteName)
        {
            case "Source":
            {
                var dataSetType = CanvasNodeTypes.ByName("DataSet");
                if (dataSetType == CanvasNodeTypes.NotFound)
                    return Unregistered(log, "DataSet");

                return Success(dataSetType, new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [PipelineCanvasMetadataKeys.DataSetRole] = PipelineCanvasMetadataKeys.RoleSource,
                });
            }

            case "Destination":
            {
                var dataSetType = CanvasNodeTypes.ByName("DataSet");
                if (dataSetType == CanvasNodeTypes.NotFound)
                    return Unregistered(log, "DataSet");

                return Success(dataSetType, new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [PipelineCanvasMetadataKeys.DataSetRole] = PipelineCanvasMetadataKeys.RoleSink,
                });
            }

            case "Map":
            case "Filter":
            case "Aggregate":
            case "Calculate":
            case "Lookup":
            {
                var transformType = CanvasNodeTypes.ByName("Transform");
                if (transformType == CanvasNodeTypes.NotFound)
                    return Unregistered(log, "Transform");

                return Success(transformType, new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [PipelineCanvasMetadataKeys.OperationType] = paletteName,
                    [PipelineCanvasMetadataKeys.ExecutionOrder] = nextExecutionOrder.ToString(CultureInfo.InvariantCulture),
                });
            }

            default:
                return GenericResult<(ICanvasNodeType NodeType, IReadOnlyDictionary<string, string> Metadata)>.Failure(
                    PipelineBuilderProviderLog.UnknownDragNodeType(log, paletteName));
        }
    }

    // Why: a registered CanvasNodeTypes member ("DataSet"/"Transform") going missing is a
    // configuration/deployment error (the type collection lost a seeded member), not a user-facing
    // unknown-palette-name case — fail loud with its own message rather than reusing
    // UnknownDragNodeType, which describes a different failure (the palette name itself).
    private static IGenericResult<(ICanvasNodeType NodeType, IReadOnlyDictionary<string, string> Metadata)> Unregistered(
        ILogger log, string nodeTypeName) =>
        GenericResult<(ICanvasNodeType NodeType, IReadOnlyDictionary<string, string> Metadata)>.Failure(
            PipelineBuilderProviderLog.CanvasNodeTypeUnregistered(log, nodeTypeName));

    private static IGenericResult<(ICanvasNodeType NodeType, IReadOnlyDictionary<string, string> Metadata)> Success(
        ICanvasNodeType nodeType,
        IReadOnlyDictionary<string, string> metadata) =>
        GenericResult<(ICanvasNodeType NodeType, IReadOnlyDictionary<string, string> Metadata)>.Success((nodeType, metadata));
}
