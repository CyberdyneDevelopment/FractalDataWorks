using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.Security;

/// <summary>
/// Answers what a person is called, for a service that records who did something (FDW-2199).
/// </summary>
/// <remarks>
/// <para>
/// The name is read from the person's own account when the service records, so it works for every way of signing in and for a token issued before this existed,
/// and it is the name as it is now. It is never taken from the token: a token carries the user id (<c>sub</c>), and a service that took a name from the context
/// recorded the id when no <c>name</c> claim was there.
/// </para>
/// <para>
/// There is no stand-in. A person with no account, or an account with no name, is a failure with a declared message, so a record that would have shown an id
/// where a name belongs is refused instead.
/// </para>
/// </remarks>
public interface IUserNames
{
    /// <summary>Reads the user name of a person.</summary>
    /// <param name="userId">The person's user id, the <c>sub</c> of their token.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The user name; a failure when the person has no account or their account has no name, or when the account could not be read.</returns>
    Task<IGenericResult<string>> Get(Guid userId, CancellationToken cancellationToken = default);
}
