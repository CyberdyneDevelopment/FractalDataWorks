using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// A first password was requested for an account that already has one. Replacing an existing
/// password is a platform-admin reset, not a first-password grant, so the two are separate acts.
/// </summary>
[TypeOption(typeof(UserResultCodes), "PasswordAlreadySet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PasswordAlreadySetCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordAlreadySetCode"/> class.
    /// </summary>
    public PasswordAlreadySetCode()
        : base(40003, "PasswordAlreadySet",
            ResultSeverities.ByName("Error"),
            "A password is already set; only a platform admin may reset it.",
            isRetryable: false)
    {
    }
}
