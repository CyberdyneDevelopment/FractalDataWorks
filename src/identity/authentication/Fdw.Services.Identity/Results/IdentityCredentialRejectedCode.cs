using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The identity provider refused this service's credential.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityCredentialRejected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityCredentialRejectedCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityCredentialRejectedCode"/> class.</summary>
    public IdentityCredentialRejectedCode()
        : base(
            51000,
            "IdentityCredentialRejected",
            ResultSeverities.ByName("Error"),
            "Identity provider rejected this service's credential: {error}.",
            isRetryable: false)
    { }
}
