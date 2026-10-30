using System.Collections.Generic;
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
        : base(name: "DataSet")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<CalculationSetting> Settings { get; } =
    [
        new CalculationSetting("dataSetName", "DataSet", true, "The data set, or the view of one, whose rows the calculation reads"),
    ];
}
