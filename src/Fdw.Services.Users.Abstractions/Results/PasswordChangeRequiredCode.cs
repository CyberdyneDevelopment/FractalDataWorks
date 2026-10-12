using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Users.Results;

/// <summary>
/// The presented password was correct, but it must be changed before a token can be issued — the
/// account was marked to change its password at next login, or the password has expired.
/// </summary>
/// <remarks>
/// Distinct from <see cref="InvalidCredentialsCode"/> on purpose. That code answers "wrong username
/// or password" identically for both, so it cannot be used to find out which usernames exist; this
/// one is only ever reached after the password was proved, so naming it leaks nothing, and a caller
/// that cannot tell it apart from a wrong password has no way to send the user to change it.
/// </remarks>
[TypeOption(typeof(UserResultCodes), "PasswordChangeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PasswordChangeRequiredCode : UserResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordChangeRequiredCode"/> class.
    /// </summary>
    public PasswordChangeRequiredCode()
        : base(51002, "PasswordChangeRequired",
            // Error, not Warning: a Warning severity counts as success, so a result carrying this code
            // read as a successful step with no outcome and the flow runner replaced it with its own
            // generic failure -- the login answered 401 and the caller never learned the password
            // had to change. This code refuses a token, like InvalidCredentials does.
            ResultSeverities.ByName("Error"),
            "The password must be changed before signing in",
            isRetryable: false)
    {
    }
}
