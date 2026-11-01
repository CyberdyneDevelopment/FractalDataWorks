using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Whether an account could sign in with a password — asked before one is issued to it.
/// </summary>
/// <remarks>
/// One question, asked the same way by every route that issues a password, so the routes cannot drift
/// apart on what "usable" means. <c>pwd</c> is the RFC 8176 method a password sign-in step proves.
/// </remarks>
public static class PasswordAdmission
{
    /// <summary>The RFC 8176 authentication method a password sign-in proves.</summary>
    public const string Method = "pwd";

    /// <summary>Asks whether any enabled password flow admits the account.</summary>
    /// <param name="admission">The admission service.</param>
    /// <param name="userId">The account.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True when a password would be usable; a failure when that could not be decided.</returns>
    public static Task<IGenericResult<bool>> Check(
        ISignInMethodAdmission admission, Guid userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(admission);
        return admission.MayUse(userId, Method, cancellationToken);
    }
}
