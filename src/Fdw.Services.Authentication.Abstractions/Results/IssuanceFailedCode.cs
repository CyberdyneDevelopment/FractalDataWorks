using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Results;

/// <summary>
/// A sign-in flow passed its terminal check, and the token or session it earned could not be issued.
/// </summary>
/// <remarks>
/// Issuance runs only after the caller's credentials and the flow's decision were accepted, so this is
/// never a refusal: it is this host's fault, and an endpoint answers it with a 5xx. Reporting it as a
/// refused sign-in would tell a caller with correct credentials that they were wrong (FDW-918).
/// </remarks>
[TypeOption(typeof(AuthenticationResultCodes), "IssuanceFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IssuanceFailedCode : AuthenticationResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IssuanceFailedCode"/> class.</summary>
    public IssuanceFailedCode()
        : base(90001, "IssuanceFailed",
            ResultSeverities.ByName("Error"),
            "The sign-in was accepted but its token or session could not be issued",
            isRetryable: false)
    {
    }
}
