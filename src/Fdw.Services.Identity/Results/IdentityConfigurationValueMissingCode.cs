using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// A required identity configuration value has no value; the domain cannot function.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityConfigurationValueMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityConfigurationValueMissingCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityConfigurationValueMissingCode"/> class.</summary>
    public IdentityConfigurationValueMissingCode()
        : base(
            61000,
            "IdentityConfigurationValueMissing",
            ResultSeverities.ByName("Critical"),
            "Identity configuration '{configurationName}' is missing required value '{property}'.",
            isRetryable: false)
    { }
}
