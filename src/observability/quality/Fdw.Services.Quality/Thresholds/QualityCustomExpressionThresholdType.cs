using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Results;

namespace Fdw.Services.Quality.Thresholds;

/// <summary>Rows an expression flags: the rule CustomExpression. A quality rule of this kind is a threshold of this type on a data set (FDW-3935).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ThresholdTypes), "QualityCustomExpression")]
public sealed class QualityCustomExpressionThresholdType : QualityThresholdTypeBase
{
    private static readonly IReadOnlyList<ThresholdField> Reported =
    [
        new ThresholdField("rowCount", typeof(long), "rows", "Rows the check read."),
        new ThresholdField("violations", typeof(long), "rows", "Rows for which the expression holds."),
    ];

    private static readonly IReadOnlyList<ThresholdParameterDeclaration> Taken =
    [
        new ThresholdParameterDeclaration("Expression", typeof(string), true, false, "The expression evaluated for each row; a row for which it holds is a violation."),
    ];

    /// <summary>Initializes a new instance of the <see cref="QualityCustomExpressionThresholdType"/> class.</summary>
    public QualityCustomExpressionThresholdType()
        : base("QualityCustomExpression", "Rows an expression flags: the rule CustomExpression.")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdField> Fields => Reported;

    /// <inheritdoc/>
    public override IReadOnlyList<ThresholdParameterDeclaration> Parameters => Taken;

    /// <inheritdoc/>
    public override IGenericResult<IReadOnlyDictionary<string, object?>> Measure(QualityMeasureInput input) => QualityMeasures.CustomExpression(input);
}
