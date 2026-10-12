using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Discovery method validation failed.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "DiscoveryValidationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DiscoveryValidationFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveryValidationFailedCode"/> class.
    /// </summary>
    public DiscoveryValidationFailedCode()
        : base(20001, "DiscoveryValidationFailed", ResultSeverities.ByName("Error"),
            "Discovery method validation failed: {ValidationErrors}",
            isRetryable: false)
    {
    }
}
