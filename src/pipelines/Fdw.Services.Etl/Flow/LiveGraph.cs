using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One flow graph the live view follows (FDW-3382): when each of its components started and ended, and, read on demand, the rows each has passed on.
/// </summary>
internal sealed class LiveGraph : IFlowLiveGraph
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Entry> _entries;
    private readonly Func<IReadOnlyList<FlowLinkCount>> _links;
    private readonly Action<LiveGraph> _closed;

    public LiveGraph(IReadOnlyList<FlowLiveComponent> components, Func<IReadOnlyList<FlowLinkCount>> links, Action<LiveGraph> closed)
    {
        ArgumentNullException.ThrowIfNull(components);
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _closed = closed ?? throw new ArgumentNullException(nameof(closed));
        _entries = components.ToDictionary(component => component.Name, component => new Entry(component), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public void ComponentStarted(string component)
    {
        lock (_gate)
        {
            Of(component).StartedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <inheritdoc />
    public void ComponentEnded(string component, bool succeeded)
    {
        lock (_gate)
        {
            var entry = Of(component);
            entry.EndedAt = DateTimeOffset.UtcNow;
            entry.Succeeded = succeeded;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _closed(this);

    /// <summary>Reads the graph's components as they are now.</summary>
    /// <returns>The components in the order the graph lists them.</returns>
    public IReadOnlyList<FlowLiveComponentView> View()
    {
        Entry[] entries;
        lock (_gate)
        {
            entries = [.. _entries.Values.Select(entry => entry.Copy())];
        }

        var links = entries.Any(entry => entry.StartedAt is not null) ? _links() : [];
        return [.. entries.Select(entry => new FlowLiveComponentView(
            entry.Component.Name, entry.Component.Stage, entry.StartedAt, entry.EndedAt, entry.Succeeded, RowsOf(entry, links), RowsInOf(entry, links)))];
    }

    // What every link of the graph has carried so far, read only when asked (FDW-3778); none while no component has started.
    public IReadOnlyList<FlowLinkCount> Links()
    {
        lock (_gate)
        {
            if (_entries.Values.All(entry => entry.StartedAt is null))
            {
                return [];
            }
        }

        return _links();
    }

    // The rows that arrived at the component, whether or not it passes any on; null until it starts (FDW-3778).
    private static long? RowsInOf(Entry entry, IReadOnlyList<FlowLinkCount> links) =>
        entry.StartedAt is null
            ? null
            : links.Where(link => string.Equals(link.Link.ToComponent, entry.Component.Name, StringComparison.OrdinalIgnoreCase)).Sum(link => link.Rows);

    private static long? RowsOf(Entry entry, IReadOnlyList<FlowLinkCount> links)
    {
        if (entry.StartedAt is null)
        {
            return null;
        }

        return entry.Component.HasOutputs
            ? links.Where(link => string.Equals(link.Link.FromComponent, entry.Component.Name, StringComparison.OrdinalIgnoreCase)).Sum(link => link.Rows)
            : links.Where(link => string.Equals(link.Link.ToComponent, entry.Component.Name, StringComparison.OrdinalIgnoreCase)).Sum(link => link.Rows);
    }

    private Entry Of(string component) => _entries.TryGetValue(component, out var entry)
        ? entry
        : throw new ArgumentException($"The graph has no component '{component}'.", nameof(component));

    private sealed class Entry
    {
        public Entry(FlowLiveComponent component)
        {
            Component = component;
        }

        public FlowLiveComponent Component { get; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? EndedAt { get; set; }

        public bool? Succeeded { get; set; }

        public Entry Copy() => new(Component) { StartedAt = StartedAt, EndedAt = EndedAt, Succeeded = Succeeded };
    }
}
