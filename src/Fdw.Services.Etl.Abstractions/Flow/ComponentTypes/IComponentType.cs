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

    /// <summary>
    /// Gets the text a client puts between the stated settings that summarise the kind (see <see cref="StageMember.SummarisesComponent"/>), in the order
    /// the kind lists them (RUI-547): <c> = </c> for a Set variable, which reads <c>count = 5</c>. A kind that flags two or more settings states one; a kind
    /// that flags fewer has nothing to join and is null.
    /// </summary>
    string? SummarySeparator { get; }
}
