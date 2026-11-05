using System;
using Fdw.Services.Pipelines.Hubs;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// A pipeline event a host that serves no hub hands to the host that does (FDW-3773). It carries
/// the same update types the hub sends to browsers, so the live view reads one shape.
/// </summary>
public sealed class PipelineEventRequest
{
    /// <summary>Gets or sets the event kind: the name of a <c>PipelineEventKinds</c> option.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the owning organization, used to scope the broadcast to that organization's
    /// firehose. Null means no organization firehose is targeted, as <c>IPipelineStatusBroadcaster</c> defines.
    /// </summary>
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Gets or sets the execution a Paused or Resumed event is about. The other kinds carry their
    /// execution in their own body.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>Gets or sets the status update, for the StatusChanged kind.</summary>
    public PipelineStatusUpdate? Status { get; set; }

    /// <summary>Gets or sets the progress update, for the Progress kind.</summary>
    public PipelineProgressUpdate? Progress { get; set; }

    /// <summary>Gets or sets the completion details, for the Completed kind.</summary>
    public PipelineExecutionComplete? Completion { get; set; }

    /// <summary>Gets or sets the task status update, for the TaskStatus kind.</summary>
    public PipelineTaskStatusUpdate? Task { get; set; }

    /// <summary>Gets or sets the components update, for the ComponentsChanged kind.</summary>
    public PipelineComponentsUpdate? Components { get; set; }

    /// <summary>Gets or sets the edge flow update, for the EdgeFlow kind.</summary>
    public PipelineEdgeFlowUpdate? EdgeFlow { get; set; }
}
