using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>'{Kind}' is not a kind of subject a threshold can be attached to</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdSubjectKindUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdSubjectKindUnknownCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdSubjectKindUnknownCode"/> class.</summary>
    public ThresholdSubjectKindUnknownCode()
        : base(
            20007,
            "ThresholdSubjectKindUnknown",
            ResultSeverities.ByName("Error"),
            "'{Kind}' is not a kind of subject a threshold can be attached to",
            isRetryable: false)
    {
    }
}
