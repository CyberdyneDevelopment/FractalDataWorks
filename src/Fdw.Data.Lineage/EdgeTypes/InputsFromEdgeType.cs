using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Lineage.EdgeTypes;

/// <summary>
/// Calculation inputs from a DataSet (Calculation←DataSet).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(LineageEdgeTypes), "InputsFrom")]
public sealed class InputsFromEdgeType : LineageEdgeTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputsFromEdgeType"/> class.
    /// </summary>
    public InputsFromEdgeType() : base(5, "InputsFrom") { }
}
