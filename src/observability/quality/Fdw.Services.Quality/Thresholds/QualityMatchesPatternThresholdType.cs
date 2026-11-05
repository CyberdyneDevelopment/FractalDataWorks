using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>Rows whose field does not match a pattern: the rule MatchesPattern. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityMatchesPattern")]
public sealed class QualityMatchesPatternThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
        new ThresholdField("violations", typeof(long), "rows", "Rows where the field does not match the pattern."),
    ];

    private static readonly IReadOnlyList<ThresholdParameterDeclaration> Taken =
    [
        new ThresholdParameterDeclaration("FieldName", typeof(string), true, false, "The field the rule reads."),
        new ThresholdParameterDeclaration("Pattern", typeof(string), true, false, "The regular expression the field must match."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityMatchesPatternThresholdType"/> class.</summary>
    public QualityMatchesPatternThresholdType()
        : base("QualityMatchesPattern", "Rows whose field does not match a pattern: the rule MatchesPattern.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdParameterDeclaration> Parameters => Taken;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.MatchesPattern(input);
}
