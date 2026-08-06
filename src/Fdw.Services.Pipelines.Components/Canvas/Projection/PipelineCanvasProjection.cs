using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Pipelines.Components.Canvas.Validation;
using Fdw.Services.Pipelines.Components.Logging;
using Fdw.UI.Abstractions.Canvas;
using Fdw.UI.Abstractions.RenderModeOptions;
using Fdw.UI.Pipelines.Clients.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas.Projection;

/// <summary>
/// Pure, render-agnostic projection between <see cref="PipelineDetailPayload"/> (the pipeline designer
/// client contract) and <see cref="PipelineCanvasModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two directions:
/// <list type="bullet">
/// <item><see cref="ToCanvas"/> — reads a <see cref="PipelineDetailPayload"/> (tasks + connections) and
/// produces a <see cref="PipelineCanvasModel"/> for display or editing.</item>
/// <item><see cref="ToDetail"/> — reads a <see cref="PipelineCanvasModel"/> and produces a new
/// <see cref="PipelineDetailPayload"/>. Runs the validator first; returns failure with the issues if the
/// graph is invalid.</item>
/// </list>
/// </para>
/// <para>
/// All methods are static. No Blazor or ASP.NET types appear here. This projection carries no
/// dependency on server-core pipeline/ETL execution types — it maps exclusively against the
/// designer client contract (<see cref="PipelineDetailPayload"/>, <see cref="TaskPayload"/>,
/// <see cref="TaskConnectionPayload"/>).
/// </para>
/// </remarks>
public static class PipelineCanvasProjection
{
    // ── Port id convention ───────────────────────────────────────────────────────
    // Why: task nodes are edit-context symmetric (one "in" + one "out" port each — see
    // PipelineCanvasEditContext.BuildPorts). TaskConnectionPayload.SourcePort/TargetPort are 0-based
    // port indices; index 0 is the (currently only) in/out port every node exposes, so it maps to
    // the "in"/"out" port id. Any non-zero index is preserved losslessly as its numeric string so a
    // future multi-port node type round-trips without silently collapsing to port 0.

    private const string InPortId = "in";
    private const string OutPortId = "out";

    // ── ToCanvas ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Projects a <see cref="PipelineDetailPayload"/> into a <see cref="PipelineCanvasModel"/>.
    /// </summary>
    /// <param name="detail">The pipeline detail (tasks + connections) from the designer client.</param>
    /// <param name="renderMode">
    /// The render mode for the resulting canvas.
    /// Pass <see cref="RenderModes.Edit"/> for an editable canvas or <see cref="RenderModes.View"/>
    /// for read-only display.
    /// </param>
    /// <param name="logger">
    /// Optional logger; defaults to <see cref="NullLogger"/> when null.
    /// </param>
    /// <returns>A populated <see cref="PipelineCanvasModel"/>.</returns>
    public static PipelineCanvasModel ToCanvas(
        PipelineDetailPayload detail,
        IRenderMode renderMode,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;
        PipelineCanvasLog.ProjectingToCanvas(log, detail.Name);

        var nodes = BuildNodes(detail.Tasks, log);
        var edges = BuildEdges(detail.Connections, log);

        return new PipelineCanvasModel(
            id: detail.Id.ToString(),
            title: detail.Name,
            renderMode: renderMode,
            nodes: nodes,
            edges: edges);
    }

    // ── ToDetail ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Projects a <see cref="PipelineCanvasModel"/> back to a <see cref="PipelineDetailPayload"/>.
    /// </summary>
    /// <remarks>
    /// Runs <see cref="PipelineGraphValidator.Validate"/> first. Returns <c>Failure</c> when the
    /// graph is invalid (issue count is logged and reported via <see cref="IGenericMessage"/>).
    /// </remarks>
    /// <param name="model">The canvas model to project.</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger"/> when null.</param>
    /// <returns>
    /// A result containing the projected <see cref="PipelineDetailPayload"/> on success, or the
    /// validation failure otherwise.
    /// </returns>
    public static IGenericResult<PipelineDetailPayload> ToDetail(
        PipelineCanvasModel model,
        ILogger? logger = null)
    {
        var log = logger ?? NullLogger.Instance;

        // ── Validate first ────────────────────────────────────────────────────
        var validation = PipelineGraphValidator.Validate(model);
        if (!validation.IsValid)
        {
            return GenericResult<PipelineDetailPayload>.Failure(
                PipelineCanvasLog.ValidationFailed(log, validation.Errors.Count()));
        }

        var taskIdByNodeId = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var tasks = BuildTasks(model, taskIdByNodeId);
        var connections = BuildConnections(model, taskIdByNodeId, log);

        return GenericResult<PipelineDetailPayload>.Success(new PipelineDetailPayload
        {
            // Why: Guid.Empty is the established "not yet assigned" sentinel for a pipeline id in
            // this package (mirrors PipelineBuilderProvider.PipelineConfigurationId) — a canvas
            // model for a not-yet-saved pipeline carries a non-Guid model.Id (e.g. "empty"/"pipe-1").
            Id = Guid.TryParse(model.Id, out var pipelineId) ? pipelineId : Guid.Empty,
            Name = model.Title,
            Tasks = tasks,
            Connections = connections,
        });
    }

    // ── Private helpers: ToCanvas ────────────────────────────────────────────────

    private static List<PipelineCanvasNode> BuildNodes(IEnumerable<TaskPayload> taskDtos, ILogger log)
    {
        var nodes = new List<PipelineCanvasNode>();

        foreach (var task in taskDtos)
        {
            var nodeType = CanvasNodeTypes.ByName(task.TaskType);
            if (nodeType == CanvasNodeTypes.NotFound)
            {
                // Why: fail loud — an unregistered task type is a data/configuration error, not
                // something to paper over with a placeholder node type. Skip the node; the task is
                // simply absent from the resulting canvas rather than silently misrepresented.
                PipelineCanvasLog.UnknownTaskNodeType(log, task.TaskType);
                continue;
            }

            nodes.Add(new PipelineCanvasNode(
                task.Id.ToString(),
                nodeType,
                task.Name,
                subLabel: task.TaskType,
                x: task.PositionX,
                y: task.PositionY,
                ports: BuildTaskPorts(),
                metadata: BuildTaskMetadata(task)));
        }

        return nodes;
    }

    private static List<PipelineCanvasEdge> BuildEdges(IEnumerable<TaskConnectionPayload> connectionDtos, ILogger log)
    {
        var edges = new List<PipelineCanvasEdge>();

        foreach (var connection in connectionDtos)
        {
            var edgeType = CanvasEdgeTypes.ByName(connection.EdgeKind);
            if (edgeType == CanvasEdgeTypes.NotFound)
            {
                // Why: fail loud — an unregistered edge kind is a data/configuration error; skip the
                // connection rather than silently defaulting it to some other edge type.
                PipelineCanvasLog.UnknownEdgeKind(log, connection.EdgeKind);
                continue;
            }

            edges.Add(new PipelineCanvasEdge(
                connection.Id.ToString(),
                connection.SourceTaskId.ToString(),
                connection.TargetTaskId.ToString(),
                edgeType,
                sourcePortId: ToPortId(connection.SourcePort, OutPortId),
                targetPortId: ToPortId(connection.TargetPort, InPortId),
                label: connection.Label));
        }

        return edges;
    }

    private static IReadOnlyList<ICanvasPort> BuildTaskPorts()
    {
        return
        [
            new PipelineCanvasPort(InPortId, "Input", PortDirections.ByName("In")!),
            new PipelineCanvasPort(OutPortId, "Output", PortDirections.ByName("Out")!),
        ];
    }

    private static Dictionary<string, string> BuildTaskMetadata(TaskPayload task)
    {
        // Why: ICanvasNode.Metadata is a string-only bag (the canvas contract has no notion of
        // TaskPayload's per-task CLR-typed Configuration values). Every entry — including id-shaped
        // (Guid) values — is stringified with invariant formatting; the reverse direction
        // (BuildConfiguration) restores plain strings, which is a known, accepted precision loss.
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in task.Configuration)
            metadata[entry.Key] = ToMetadataValue(entry.Value);

        return metadata;
    }

    private static string ToMetadataValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string s => s,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    private static string ToPortId(int portIndex, string zeroPortId) =>
        portIndex == 0 ? zeroPortId : portIndex.ToString(CultureInfo.InvariantCulture);

    // ── Private helpers: ToDetail ─────────────────────────────────────────────────

    private static List<TaskPayload> BuildTasks(PipelineCanvasModel model, Dictionary<string, Guid> taskIdByNodeId)
    {
        var tasks = new List<TaskPayload>();

        foreach (var node in model.Nodes.OfType<PipelineCanvasNode>())
        {
            // Why: a loaded task's canvas node id is its original TaskPayload.Id (round-tripped from
            // ToCanvas). A node added during this editing session carries an edit-context-assigned
            // sequence id (e.g. "pnode-1" — see PipelineCanvasEditContext.NextNodeId) which has never
            // been persisted, so a fresh identity is minted for it here.
            var taskId = Guid.TryParse(node.Id, out var parsedId) ? parsedId : Guid.CreateVersion7();
            taskIdByNodeId[node.Id] = taskId;

            tasks.Add(new TaskPayload
            {
                Id = taskId,
                Name = node.Label,
                TaskType = node.NodeType.Name,
                PositionX = node.X,
                PositionY = node.Y,
                Configuration = BuildConfiguration(node.Metadata),
            });
        }

        return tasks;
    }

    private static List<TaskConnectionPayload> BuildConnections(
        PipelineCanvasModel model,
        Dictionary<string, Guid> taskIdByNodeId,
        ILogger log)
    {
        var connections = new List<TaskConnectionPayload>();

        foreach (var edge in model.Edges.OfType<PipelineCanvasEdge>())
        {
            if (!taskIdByNodeId.TryGetValue(edge.SourceNodeId, out var sourceTaskId))
            {
                PipelineCanvasLog.NodeNotFound(log, edge.SourceNodeId);
                continue;
            }

            if (!taskIdByNodeId.TryGetValue(edge.TargetNodeId, out var targetTaskId))
            {
                PipelineCanvasLog.NodeNotFound(log, edge.TargetNodeId);
                continue;
            }

            connections.Add(new TaskConnectionPayload
            {
                Id = Guid.TryParse(edge.Id, out var edgeId) ? edgeId : Guid.CreateVersion7(),
                SourceTaskId = sourceTaskId,
                SourcePort = ToPortIndex(edge.SourcePortId, OutPortId),
                TargetTaskId = targetTaskId,
                TargetPort = ToPortIndex(edge.TargetPortId, InPortId),
                Label = edge.Label,
                EdgeKind = edge.EdgeType.Name,
            });
        }

        return connections;
    }

    private static Dictionary<string, object?> BuildConfiguration(IReadOnlyDictionary<string, string> metadata)
    {
        // Why: the reverse of BuildTaskMetadata — the canvas metadata bag is string-only, so the
        // original CLR type of each TaskPayload.Configuration value (int/bool/Guid/etc.) cannot be
        // recovered here. Values come back as plain strings; a caller needing a typed value must
        // re-parse using TaskType-specific knowledge this projection intentionally doesn't have.
        var configuration = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var entry in metadata)
            configuration[entry.Key] = entry.Value;

        return configuration;
    }

    private static int ToPortIndex(string? portId, string zeroPortId)
    {
        if (string.Equals(portId, zeroPortId, StringComparison.Ordinal))
            return 0;

        return int.TryParse(portId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index) ? index : 0;
    }
}
