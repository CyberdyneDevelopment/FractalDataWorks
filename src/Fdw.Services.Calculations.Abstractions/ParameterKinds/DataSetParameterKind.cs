using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Parameter kind representing a DataSet reference.
/// </summary>
[TypeOption(typeof(OperationParameterKinds), "DataSet")]
[ExcludeFromCodeCoverage]
public sealed class DataSetParameterKind : OperationParameterKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetParameterKind"/> class.
    /// </summary>
    public DataSetParameterKind()
        : base(id: 4, name: "DataSet", description: "A DataSet reference")
    {
    }
}
