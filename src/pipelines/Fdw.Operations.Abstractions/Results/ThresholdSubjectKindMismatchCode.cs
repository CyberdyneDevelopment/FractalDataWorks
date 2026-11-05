using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>Threshold '{Threshold}' judges {ThresholdKind} subjects but is attached to a {Kind}</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdSubjectKindMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdSubjectKindMismatchCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdSubjectKindMismatchCode"/> class.</summary>
    public ThresholdSubjectKindMismatchCode()
        : base(
            20008,
            "ThresholdSubjectKindMismatch",
            ResultSeverities.ByName("Error"),
            "Threshold '{Threshold}' judges {ThresholdKind} subjects but is attached to a {Kind}",
            isRetryable: false)
    {
    }
}
