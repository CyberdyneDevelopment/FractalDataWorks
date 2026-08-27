using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// No parent configuration provider registered — service lookup cannot proceed.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "NoDomainConfigurationProvider", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoDomainConfigurationProviderCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoDomainConfigurationProviderCode"/> class.
    /// </summary>
    public NoDomainConfigurationProviderCode()
        : base(61003, "NoDomainConfigurationProvider",
            ResultSeverities.ByName("Error"),
            "No parent configuration provider registered — cannot resolve '{Identifier}'",
            isRetryable: false)
    {
    }
}
