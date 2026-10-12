using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The provider issued a token with fewer scopes than were asked for.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityScopesNarrowed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityScopesNarrowedCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityScopesNarrowedCode"/> class.</summary>
    public IdentityScopesNarrowedCode()
        : base(
            51001,
            "IdentityScopesNarrowed",
            ResultSeverities.ByName("Warning"),
            "Identity token granted narrower scopes than requested: granted [{granted}].",
            isRetryable: false)
    { }
}
