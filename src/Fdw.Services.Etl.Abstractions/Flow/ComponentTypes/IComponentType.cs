using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// One kind of component a pipeline graph is built from: a source, a transformation, a join, a destination, or an action or loop (FDW-1395, FDW-2041; see <see cref="IsAction"/>).
/// </summary>
public interface IComponentType : ITypeOption<int, IComponentType>, IFlowComponentShape
{
    /// <summary>Gets whether the kind is an action or a loop (FDW-2041): it moves no rows and runs as a step of the flow, between the stages around it.</summary>
    bool IsAction { get; }

    /// <summary>Gets whether the kind is a loop, which runs the components that name it as their parent once for each round.</summary>
    bool HoldsComponents { get; }
}
