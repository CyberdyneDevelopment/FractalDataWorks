using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Parameter kind representing a single field from a DataSet.
/// </summary>
[TypeOption(typeof(OperationParameterKinds), "Field")]
[ExcludeFromCodeCoverage]
public sealed class FieldParameterKind : OperationParameterKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldParameterKind"/> class.
    /// </summary>
    public FieldParameterKind()
        : base(id: 2, name: "Field", description: "A single field from a DataSet")
    {
    }
}
