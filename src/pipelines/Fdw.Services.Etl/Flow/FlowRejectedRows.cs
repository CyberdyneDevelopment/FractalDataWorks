using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// Counts the rows that leave through the error outputs of a run, and the reasons they carry, and keeps no row (FDW-1720).
/// </summary>
/// <remarks>
/// Why no row: a row can hold a private value, and the record of the run is read by more people than the data is. What a person needs
/// there is how many rows were rejected and why, so that is all that is kept. A pipeline that wants the rows writes them: an error
/// output that is linked goes to a destination the pipeline's author chose. The reasons are the components' own wording; the most
/// distinct reasons kept for one output is <see cref="MostReasons"/>, so a component that puts a value into each reason cannot grow the
/// record without end. Components run at the same time, so the counting is done under a lock; it observes the run and never changes
/// what the run does.
/// </remarks>
internal sealed class FlowRejectedRows
{
    /// <summary>The most distinct reasons kept for one error output of one component.</summary>
    internal const int MostReasons = 20;

    private readonly Lock _gate = new();
    private readonly Dictionary<(string Component, string Output), Counted> _counted = new();

    /// <summary>Counts the rows of a batch that left through an error output and tallies their reasons.</summary>
    /// <param name="component">The component that wrote them.</param>
    /// <param name="output">The error output node they left through.</param>
    /// <param name="rows">The rows; none is kept.</param>
    public void Keep(string component, string output, IReadOnlyList<IDictionary<string, object?>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            return;
        }

        lock (_gate)
        {
            var key = (component.ToUpperInvariant(), output.ToUpperInvariant());
            if (!_counted.TryGetValue(key, out var counted))
            {
                counted = new Counted(component, output);
                _counted[key] = counted;
            }

            foreach (var row in rows)
            {
                counted.Count++;
                if (row.TryGetValue(FlowRowMapper.ErrorReasonField, out var stated) && stated is string { Length: > 0 } reason)
                {
                    counted.Tally(reason);
                }
            }
        }
    }

    /// <summary>Gets what has been counted so far, by component and then output.</summary>
    /// <returns>One entry for each error output that carried at least one row.</returns>
    public IReadOnlyList<FlowRejection> Snapshot()
    {
        lock (_gate)
        {
            return
            [
                .. _counted.Values
                    .OrderBy(counted => counted.Component, StringComparer.Ordinal)
                    .ThenBy(counted => counted.Output, StringComparer.Ordinal)
                    .Select(counted => new FlowRejection(counted.Component, counted.Output, counted.Count, counted.Reasons())),
            ];
        }
    }

    private sealed class Counted(string component, string output)
    {
        private readonly Dictionary<string, long> _reasons = new(StringComparer.Ordinal);

        public string Component { get; } = component;

        public string Output { get; } = output;

        public long Count { get; set; }

        public void Tally(string reason)
        {
            if (_reasons.TryGetValue(reason, out var seen))
            {
                _reasons[reason] = seen + 1;
            }
            else if (_reasons.Count < MostReasons)
            {
                _reasons[reason] = 1;
            }
        }

        public IReadOnlyList<FlowRejectionReason> Reasons() =>
            [.. _reasons.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new FlowRejectionReason(pair.Key, pair.Value))];
    }
}
