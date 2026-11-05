using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Operations.Abstractions.Results;

/// <summary>{Kind} '{Subject}' already has threshold '{Threshold}' attached.</summary>
[TypeOption(typeof(OperationsResultCodes), "ThresholdLinkExists", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ThresholdLinkExistsCode : OperationsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdLinkExistsCode"/> class.</summary>
    public ThresholdLinkExistsCode()
        : base(
            41001,
            "ThresholdLinkExists",
            ResultSeverities.ByName("Error"),
            "{Kind} '{Subject}' already has threshold '{Threshold}' attached",
            isRetryable: false)
    {
    }
}
