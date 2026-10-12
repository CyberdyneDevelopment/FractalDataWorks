using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The create-user request named no sign-in method. An account with no password, no external
/// identity and no key has no way in, so the absence is refused rather than defaulted.
/// </summary>
[TypeOption(typeof(UserResultCodes), "NoSignInMethod", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoSignInMethodCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoSignInMethodCode"/> class.
    /// </summary>
    public NoSignInMethodCode()
        : base(20000, "NoSignInMethod",
            ResultSeverities.ByName("Error"),
            "At least one sign-in method is required: a password, an external identity, or an API key.",
            isRetryable: false)
    {
    }
}
