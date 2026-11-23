using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>A threshold was attached to a subject that does not exist: no {Kind} '{Subject}' is current (FDW-6221).</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdSubjectNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdSubjectNotFoundCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdSubjectNotFoundCode"/> class.</summary>
    public ThresholdSubjectNotFoundCode()
        : base(
            31005,
            "ThresholdSubjectNotFound",
            ResultSeverities.ByName("Error"),
            "No {Kind} '{Subject}' was found, so threshold '{Threshold}' cannot be attached to it",
            isRetryable: false)
    {
    }
}
