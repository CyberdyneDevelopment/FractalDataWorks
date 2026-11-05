using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>Rows whose field is not in an allowed set: the rule InReferenceSet. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityInReferenceSet")]
public sealed class QualityInReferenceSetThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
        new ThresholdField("violations", typeof(long), "rows", "Rows where the field is not one of the allowed values."),
    ];

    private static readonly IReadOnlyList<ThresholdParameterDeclaration> Taken =
    [
        new ThresholdParameterDeclaration("FieldName", typeof(string), true, false, "The field the rule reads."),
        new ThresholdParameterDeclaration("AllowedValues", typeof(string), true, true, "The allowed values, in order."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityInReferenceSetThresholdType"/> class.</summary>
    public QualityInReferenceSetThresholdType()
        : base("QualityInReferenceSet", "Rows whose field is not in an allowed set: the rule InReferenceSet.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdParameterDeclaration> Parameters => Taken;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.InReferenceSet(input);
}
