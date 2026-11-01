using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The identity provider could not be reached.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityProviderUnreachable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityProviderUnreachableCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityProviderUnreachableCode"/> class.</summary>
    public IdentityProviderUnreachableCode()
        : base(
            71000,
            "IdentityProviderUnreachable",
            ResultSeverities.ByName("Error"),
            "Could not reach identity provider at '{issuer}'.",
            isRetryable: true)
    { }
}
