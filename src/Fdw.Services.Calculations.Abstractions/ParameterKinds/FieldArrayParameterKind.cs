using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Parameter kind representing multiple fields from a DataSet (e.g., GroupBy, OrderBy).
/// </summary>
[TypeOption(typeof(OperationParameterKinds), "FieldArray")]
[ExcludeFromCodeCoverage]
public sealed class FieldArrayParameterKind : OperationParameterKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldArrayParameterKind"/> class.
    /// </summary>
    public FieldArrayParameterKind()
        : base(id: 3, name: "FieldArray", description: "Multiple fields from a DataSet (e.g., GroupBy)")
    {
    }
}
