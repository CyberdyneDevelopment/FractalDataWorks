using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The identity provider answered with an unexpected status.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityProviderReturnedError", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityProviderReturnedErrorCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityProviderReturnedErrorCode"/> class.</summary>
    public IdentityProviderReturnedErrorCode()
        : base(
            71001,
            "IdentityProviderReturnedError",
            ResultSeverities.ByName("Error"),
            "Identity provider returned {statusCode}.",
            isRetryable: true)
    { }
}
