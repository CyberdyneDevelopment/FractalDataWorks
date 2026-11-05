using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>The number of distinct values a field holds: the rule DistinctCountInRange. The limits are conditions on distinctCount. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityDistinctCountInRange")]
public sealed class QualityDistinctCountInRangeThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
        new ThresholdField("distinctCount", typeof(long), "values", "How many distinct values the field holds."),
    ];

    private static readonly IReadOnlyList<ThresholdParameterDeclaration> Taken =
    [
        new ThresholdParameterDeclaration("FieldName", typeof(string), true, false, "The field the rule reads."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityDistinctCountInRangeThresholdType"/> class.</summary>
    public QualityDistinctCountInRangeThresholdType()
        : base("QualityDistinctCountInRange", "The number of distinct values a field holds: the rule DistinctCountInRange. The limits are conditions on distinctCount.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdParameterDeclaration> Parameters => Taken;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.DistinctCount(input);
}
