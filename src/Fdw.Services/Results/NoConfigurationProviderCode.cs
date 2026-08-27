using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// No configuration provider registered for the resolved ServiceOptionType.
/// </summary>
/// <remarks>
/// The parent provider yields only the child's Id and ServiceOptionType; the configuration itself comes
/// from the provider registered for that type. A missing registration is a wiring defect, not a case to
/// route around by reading the configuration off the parent record.
/// </remarks>
[TypeOption(typeof(ServicesResultCodes), "NoConfigurationProvider", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoConfigurationProviderCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoConfigurationProviderCode"/> class.
    /// </summary>
    public NoConfigurationProviderCode()
        : base(61005, "NoConfigurationProvider",
            ResultSeverities.ByName("Error"),
            "No configuration provider registered for ServiceOptionType '{ServiceOptionType}' — cannot resolve '{Identifier}'",
            isRetryable: false)
    {
    }
}
