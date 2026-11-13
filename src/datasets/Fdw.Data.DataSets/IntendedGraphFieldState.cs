using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>Nothing feeds the field yet, and its data set states what it intends to read.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphFieldStates), "Intended")]
public sealed class IntendedGraphFieldState : GraphFieldStateBase
{
    /// <summary>Initializes a new instance of the <see cref="IntendedGraphFieldState"/> class.</summary>
    public IntendedGraphFieldState() : base("Intended", precedence: 2)
    {
    }

    /// <inheritdoc />
    public override bool Applies(GraphDataSetFieldConfiguration field, bool dataSetHasIntendedSource) =>
        field.FedByComponent is null && dataSetHasIntendedSource;
}
