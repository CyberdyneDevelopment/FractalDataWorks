using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Input kind representing a DataSet as the source of calculation input.
/// </summary>
[TypeOption(typeof(CalculationInputKinds), "DataSet")]
[ExcludeFromCodeCoverage]
public sealed class DataSetCalculationInputKind : CalculationInputKindBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetCalculationInputKind"/> class.
    /// </summary>
    public DataSetCalculationInputKind()
        : base(id: 1, name: "DataSet")
    {
    }
}
