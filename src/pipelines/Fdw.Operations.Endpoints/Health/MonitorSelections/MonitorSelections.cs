using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>
/// The ways the HealthMonitor domain can answer which monitor a host reports to: it names none, exactly one, or several.
/// </summary>
/// <remarks>Exactly one answers; none and several are each refused with their own coded message, because picking by order would make the monitor depend on read order.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(MonitorSelectionBase), typeof(IMonitorSelection), typeof(MonitorSelections))]
public abstract partial class MonitorSelections : TypeCollectionBase<MonitorSelectionBase, IMonitorSelection>
{
    /// <summary>Finds the answer for a domain that names this many monitors.</summary>
    /// <param name="count">How many monitors the domain names.</param>
    /// <returns>The selection that holds the count.</returns>
    public static IMonitorSelection Of(int count) => All().First(selection => selection.Matches(count));
}
