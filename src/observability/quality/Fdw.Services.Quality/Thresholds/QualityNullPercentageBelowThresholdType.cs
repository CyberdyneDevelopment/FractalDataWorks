using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>The share of rows whose field is null: the rule NullPercentageBelow. The limit is a condition on nullPercentage. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityNullPercentageBelow")]
public sealed class QualityNullPercentageBelowThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
        new ThresholdField("nullPercentage", typeof(double), "percent", "Percentage of rows where the field is null, 0 to 100."),
    ];

    private static readonly IReadOnlyList<ThresholdParameterDeclaration> Taken =
    [
        new ThresholdParameterDeclaration("FieldName", typeof(string), true, false, "The field the rule reads."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityNullPercentageBelowThresholdType"/> class.</summary>
    public QualityNullPercentageBelowThresholdType()
        : base("QualityNullPercentageBelow", "The share of rows whose field is null: the rule NullPercentageBelow. The limit is a condition on nullPercentage.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdParameterDeclaration> Parameters => Taken;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.NullPercentage(input);
}
