using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Quality threshold '{Threshold}' could not take its measure: {Reason}.</summary>
[TypeOption(typeof(OperationsResultCodes), "QualityMeasureFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QualityMeasureFailedCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="QualityMeasureFailedCode"/> class.</summary>
    public QualityMeasureFailedCode()
        : base(
            20011,
            "QualityMeasureFailed",
            ResultSeverities.ByName("Error"),
            "Quality threshold '{Threshold}' could not take its measure: {Reason}",
            isRetryable: false)
    {
    }
}
