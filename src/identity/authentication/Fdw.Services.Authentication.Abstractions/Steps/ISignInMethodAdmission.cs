using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Answers whether a user could sign in with a given method, without running a flow.
/// </summary>
/// <remarks>
/// <para>
/// A flow decides at sign-in, through its gated steps. Some decisions have to be made earlier: an
/// administrator setting somebody's password is issuing a credential, and a credential nobody can
/// use is a credential nobody should hold. This asks the same question the flows would, from the same
/// place — each enabled flow's <see cref="AuthenticationFlow.RequiredPermissions"/>.
/// </para>
/// <para>
/// A method is the RFC 8176 value a flow's steps declare they prove, e.g. <c>pwd</c> for a password.
/// The user is admitted when at least one enabled flow proving that method admits them: a flow with no
/// requirement admits every active user, and a gated flow admits a user holding all its permissions in
/// the tenant their sign-in lands in.
/// </para>
/// </remarks>
public interface ISignInMethodAdmission
{
    /// <summary>Decides whether <paramref name="userId"/> could sign in by <paramref name="authenticationMethod"/>.</summary>
    /// <param name="userId">The user.</param>
    /// <param name="authenticationMethod">The RFC 8176 method, e.g. <c>pwd</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// True when some enabled flow proving the method admits the user; false when none does, including
    /// when no enabled flow proves the method at all. A failure means the question could not be
    /// answered, which a caller must not read as yes.
    /// </returns>
    Task<IGenericResult<bool>> MayUse(Guid userId, string authenticationMethod, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decides whether a principal holding <paramref name="heldPermissions"/> could sign in by
    /// <paramref name="authenticationMethod"/> — for an account that does not exist yet, whose
    /// permissions are those of the roles it is about to be given.
    /// </summary>
    /// <param name="heldPermissions">The permissions the principal would hold where its sign-in lands.</param>
    /// <param name="authenticationMethod">The RFC 8176 method, e.g. <c>pwd</c>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>As the user overload: false includes "no enabled flow proves the method".</returns>
    Task<IGenericResult<bool>> MayUse(
        IReadOnlyCollection<string> heldPermissions, string authenticationMethod, CancellationToken cancellationToken = default);
}
