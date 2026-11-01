using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// A password was asked for an account that no enabled password sign-in flow would admit (FDW-910).
/// </summary>
/// <remarks>
/// A state of the account's roles, not bad input: grant a role carrying what the password flow
/// requires (the seeded PasswordSignIn role carries signin:password) and the same request succeeds.
/// Refused rather than issued, because a password nobody can sign in with is a credential that should
/// not exist.
/// </remarks>
[TypeOption(typeof(UserResultCodes), "PasswordNotUsable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PasswordNotUsableCode : UserResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="PasswordNotUsableCode"/> class.</summary>
    public PasswordNotUsableCode()
        : base(40005, "PasswordNotUsable",
            ResultSeverities.ByName("Error"),
            "No enabled password sign-in flow admits an account holding these roles",
            isRetryable: false)
    {
    }
}
