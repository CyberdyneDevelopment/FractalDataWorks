using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The token request did not name an audience, or was otherwise unusable.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityRequestInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityRequestInvalidCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityRequestInvalidCode"/> class.</summary>
    public IdentityRequestInvalidCode()
        : base(
            21000,
            "IdentityRequestInvalid",
            ResultSeverities.ByName("Error"),
            "Identity token request is invalid: {reason}.",
            isRetryable: false)
    { }
}
