namespace Fdw.Services.Etl.Flow;

/// <summary>Ports that carry the gauge of what their component holds (FDW-5231).</summary>
internal interface IFlowHeldPorts
{
    /// <summary>Gets the component's gauge, or <see langword="null"/> when these ports carry none.</summary>
    FlowHeldGauge? Held { get; }
}
