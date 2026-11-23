using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// The one place the live view of a run becomes the wire shape (FDW-3382, FDW-3778): the queue endpoint's poll and the
/// components publisher's push both read it, so the console sees one shape from either.
/// </summary>
public static class FlowLiveResponses
{
    /// <summary>What the byte figures on the wire measure (FDW-5232): an estimate of the memory the row values hold from their types (RowSize), not process memory.</summary>
    public const string EstimatedValueBytes = "EstimatedValueBytes";

    /// <summary>The components of a run as the wire shape.</summary>
    /// <param name="views">The components the live view lists.</param>
    public static List<PipelineRunComponentResponse> Components(IEnumerable<FlowLiveComponentView> views) =>
        [.. views.Select(component => new PipelineRunComponentResponse
        {
            Name = component.Name,
            Stage = component.Stage,
            StartedAt = component.StartedAt,
            EndedAt = component.EndedAt,
            Succeeded = component.Succeeded,
            Rows = component.Rows,
            RowsIn = component.RowsIn
        })];

    /// <summary>
    /// The components of a run as the wire shape, with what each holds and how long it has waited writing, summed over its outgoing links (FDW-5232).
    /// </summary>
    /// <param name="views">The components the live view lists.</param>
    /// <param name="links">What every link of the run has carried and waits in it now.</param>
    public static List<PipelineRunComponentResponse> Components(IEnumerable<FlowLiveComponentView> views, IReadOnlyList<FlowLinkCount> links) =>
        [.. views.Select(component => new PipelineRunComponentResponse
        {
            Name = component.Name,
            Stage = component.Stage,
            StartedAt = component.StartedAt,
            EndedAt = component.EndedAt,
            Succeeded = component.Succeeded,
            Rows = component.Rows,
            RowsIn = component.RowsIn,
            HeldBytes = component.HeldBytes,
            HeldRows = component.HeldRows,
            PeakHeldBytes = component.PeakHeldBytes,
            BlockedMs = component.StartedAt is null ? null : BlockedMsOf(component.Name, links),
        })];

    /// <summary>The links of a run as the wire shape: what each has carried, what waits in it now and how long its writers have waited (FDW-5232).</summary>
    /// <param name="links">What every link has carried so far.</param>
    public static List<PipelineRunLinkResponse> Links(IEnumerable<FlowLinkCount> links) =>
        [.. links.Select(link => new PipelineRunLinkResponse
        {
            From = link.Link.FromComponent,
            To = link.Link.ToComponent,
            Rows = link.Rows,
            Bytes = link.Bytes,
            Batches = link.Batches,
            QueuedBytes = link.QueuedBytes,
            QueuedBatches = link.QueuedBatches,
            BlockedMs = (long)link.Blocked.TotalMilliseconds,
        })];

    /// <summary>
    /// What a run holds now (FDW-5232): the bytes waiting in its inputs, each input counted once however many links feed it, and the bytes its started components hold.
    /// </summary>
    /// <param name="views">The components the live view lists.</param>
    /// <param name="links">What every link of the run has carried and waits in it now.</param>
    /// <returns>The run's figures.</returns>
    public static FlowRunMemory Memory(IEnumerable<FlowLiveComponentView> views, IEnumerable<FlowLinkCount> links)
    {
        var queued = links
            .Where(link => link.Link.ToComponent.Length > 0)
            .GroupBy(link => (link.Link.ToComponent.ToUpperInvariant(), link.Link.ToInput.ToUpperInvariant()))
            .Sum(input => input.First().QueuedBytes);
        var held = views.Sum(component => component.HeldBytes is { } bytes ? bytes : 0);
        return new FlowRunMemory(queued, held);
    }

    // The time a component has waited writing: its outgoing links' waits together.
    private static long BlockedMsOf(string component, IReadOnlyList<FlowLinkCount> links) =>
        (long)links
            .Where(link => string.Equals(link.Link.FromComponent, component, StringComparison.OrdinalIgnoreCase))
            .Sum(link => link.Blocked.TotalMilliseconds);
}
