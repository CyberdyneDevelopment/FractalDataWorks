using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Flow;

/// <summary>Finds the gauge a component counts what it holds on (FDW-5231).</summary>
internal static class FlowHeld
{
    /// <summary>Gets the gauge the ports carry.</summary>
    /// <param name="ports">The ports the component runs against.</param>
    /// <returns>The gauge, or <see langword="null"/> for ports that carry none (ports a test builds), where nothing is counted.</returns>
    public static FlowHeldGauge? Of(IFlowPorts ports) => ports is IFlowHeldPorts held ? held.Held : null;
}
