using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>A threshold was attached to a subject that was retired or deleted: {Kind} '{Subject}' is no longer current (FDW-6221).</summary>
/// <remarks>
/// A subject that is gone is a different answer from one that was never there (<c>ThresholdSubjectNotFound</c>). Thresholds a subject already carries when it is
/// retired stay readable and can be detached.
/// </remarks>
[TypeOption(typeof(OperationsResultCodes), "ThresholdSubjectRetired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdSubjectRetiredCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdSubjectRetiredCode"/> class.</summary>
    public ThresholdSubjectRetiredCode()
        : base(
            41005,
            "ThresholdSubjectRetired",
            ResultSeverities.ByName("Error"),
            "The {Kind} '{Subject}' was retired, so threshold '{Threshold}' cannot be attached to it",
            isRetryable: false)
    {
    }
}
