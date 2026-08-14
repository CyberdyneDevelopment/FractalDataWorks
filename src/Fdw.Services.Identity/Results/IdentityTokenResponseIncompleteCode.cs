using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Identity.Results;

/// <summary>
/// The token response was well-formed but missing a required field.
/// </summary>
[TypeOption(typeof(IdentityResultCodes), "IdentityTokenResponseIncomplete", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityTokenResponseIncompleteCode : IdentityResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityTokenResponseIncompleteCode"/> class.</summary>
    public IdentityTokenResponseIncompleteCode()
        : base(
            91001,
            "IdentityTokenResponseIncomplete",
            ResultSeverities.ByName("Error"),
            "Token response carried no '{field}'.",
            isRetryable: false)
    { }
}
