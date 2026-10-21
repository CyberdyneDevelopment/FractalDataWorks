namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// The input and output nodes a component runs against (FDW-1386).
/// </summary>
public interface IFlowPorts
{
    /// <summary>Gets one of the component's input nodes.</summary>
    /// <param name="name">A name the component declared in <see cref="IFlowComponent.Inputs"/>.</param>
    /// <returns>The input node.</returns>
    IFlowInput Input(string name);

    /// <summary>Gets one of the component's output nodes.</summary>
    /// <param name="name">A name the component declared in <see cref="IFlowComponent.Outputs"/>.</param>
    /// <returns>The output node.</returns>
    IFlowOutput Output(string name);
}
