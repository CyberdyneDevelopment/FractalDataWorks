using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>The number of rows a data set holds: the rule RowCountInRange. The limits are conditions on rowCount. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityRowCountInRange")]
public sealed class QualityRowCountInRangeThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityRowCountInRangeThresholdType"/> class.</summary>
    public QualityRowCountInRangeThresholdType()
        : base("QualityRowCountInRange", "The number of rows a data set holds: the rule RowCountInRange. The limits are conditions on rowCount.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.RowCount(input);
}
