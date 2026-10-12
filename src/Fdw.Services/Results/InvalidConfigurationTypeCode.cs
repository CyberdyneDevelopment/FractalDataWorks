using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// A configuration object of the wrong concrete type was supplied to a provider's
/// configuration-based Get — the provider's domain configuration type was expected.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "InvalidConfigurationType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidConfigurationTypeCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidConfigurationTypeCode"/> class.
    /// </summary>
    public InvalidConfigurationTypeCode()
        : base(60003, "InvalidConfigurationType",
            ResultSeverities.ByName("Error"),
            "Invalid configuration type: expected '{ExpectedType}', actual '{ActualType}'",
            isRetryable: false)
    {
    }
}
