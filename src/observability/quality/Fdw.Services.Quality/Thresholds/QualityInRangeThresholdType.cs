using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>Rows whose field falls outside a range: the rule InRange. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityInRange")]
public sealed class QualityInRangeThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
        new ThresholdField("violations", typeof(long), "rows", "Rows where the field is below the lowest or above the highest allowed value."),
    ];

    private static readonly IReadOnlyList<ThresholdParameterDeclaration> Taken =
    [
        new ThresholdParameterDeclaration("FieldName", typeof(string), true, false, "The field the rule reads."),
        new ThresholdParameterDeclaration("MinValue", typeof(string), false, false, "The lowest allowed value, read as the field type."),
        new ThresholdParameterDeclaration("MaxValue", typeof(string), false, false, "The highest allowed value, read as the field type."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityInRangeThresholdType"/> class.</summary>
    public QualityInRangeThresholdType()
        : base("QualityInRange", "Rows whose field falls outside a range: the rule InRange.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdParameterDeclaration> Parameters => Taken;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.InRange(input);
}
