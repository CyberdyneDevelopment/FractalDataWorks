using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// What a component kind says about itself so the console can draw and wire it without a release (FDW-1395): its nodes and the
/// settings it takes.
/// </summary>
/// <remarks>
/// <para>
/// The type-collection listing reads this shape. The nodes appear as <c>inputs</c> and <c>outputs</c>, and the settings as the
/// option's <c>parameters</c>, as they do for a stage kind's members. A setting's kind is what the console renders to collect
/// it: <c>Text</c>, <c>Number</c>, <c>Boolean</c>, <c>Expression</c>, <c>ColumnList</c>, <c>FieldMaps</c>, <c>DataSet</c>,
/// <c>DataStore</c>, <c>Container</c>, <c>Pipeline</c>, <c>Variable</c> and <c>Steps</c>, or the name of a type collection when the
/// setting is a choice from it (<c>LoadStepKinds</c>, <c>JoinKinds</c>, <c>SplitRoutings</c>).
/// </para>
/// <para>
/// A kind with no inputs is a source; one with no outputs is a destination. That follows from its nodes and is never stated twice.
/// </para>
/// </remarks>
public interface IFlowComponentShape
{
    /// <summary>Gets the input nodes of the kind.</summary>
    IReadOnlyList<FlowNodeSpec> Inputs { get; }

    /// <summary>Gets the output nodes of the kind, error outputs included.</summary>
    IReadOnlyList<FlowNodeSpec> Outputs { get; }

    /// <summary>Gets the settings the kind takes; a setting a component must state is required, and none has a default.</summary>
    IReadOnlyList<StageMember> Settings { get; }
}
