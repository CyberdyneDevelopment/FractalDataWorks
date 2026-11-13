using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>The field is named and described: no question is open and nothing is said about where its value comes from. Every field is at least this.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(GraphFieldStates), "Defined")]
public sealed class DefinedGraphFieldState : GraphFieldStateBase
{
    /// <summary>Initializes a new instance of the <see cref="DefinedGraphFieldState"/> class.</summary>
    public DefinedGraphFieldState() : base("Defined", precedence: 3)
    {
    }

    /// <inheritdoc />
    public override bool Applies(GraphDataSetFieldConfiguration field, bool dataSetHasIntendedSource) => true;
}
