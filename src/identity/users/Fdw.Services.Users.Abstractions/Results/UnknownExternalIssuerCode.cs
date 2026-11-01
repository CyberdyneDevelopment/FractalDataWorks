using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The binding names an issuer this platform cannot resolve. A binding is matched against the
/// issuer a proved subject carries, so one naming anything else writes, reads back, and never
/// resolves a sign-in — which is indistinguishable from a typo and worse than a refusal.
/// </summary>
[TypeOption(typeof(UserResultCodes), "UnknownExternalIssuer", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UnknownExternalIssuerCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnknownExternalIssuerCode"/> class.
    /// </summary>
    public UnknownExternalIssuerCode()
        : base(20002, "UnknownExternalIssuer",
            ResultSeverities.ByName("Error"),
            "The external identity names an issuer this platform cannot resolve.",
            isRetryable: false)
    {
    }
}
