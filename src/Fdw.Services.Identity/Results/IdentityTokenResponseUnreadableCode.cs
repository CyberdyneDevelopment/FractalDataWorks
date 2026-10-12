using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The provider's token response was not parseable JSON.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityTokenResponseUnreadable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityTokenResponseUnreadableCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityTokenResponseUnreadableCode"/> class.</summary>
    public IdentityTokenResponseUnreadableCode()
        : base(
            91000,
            "IdentityTokenResponseUnreadable",
            ResultSeverities.ByName("Error"),
            "Could not read the token response from '{issuer}'.",
            isRetryable: false)
    { }
}
