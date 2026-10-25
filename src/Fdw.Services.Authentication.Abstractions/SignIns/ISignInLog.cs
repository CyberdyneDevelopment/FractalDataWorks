using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Authentication.Abstractions.SignIns;

/// <summary>
/// The log of who signed in and when (FDW-2729): one row for every sign-in, kept in AuthDb apart from the user's configuration,
/// because a sign-in is an observation and not a change to who the person is.
/// </summary>
/// <remarks>
/// A refused or suspended sign-in is not recorded. An API key or a personal access token is recorded on every request it authenticates. Each
/// row also carries who was calling (<see cref="ISignInCaller"/>), read from the request in hand and not passed in by the caller of
/// <see cref="Record"/>.
/// </remarks>
public interface ISignInLog
{
    /// <summary>Records that a person signed in.</summary>
    /// <param name="userId">The person.</param>
    /// <param name="serviceName">The Session authentication service the sign-in was issued into; null when the flow issued a bearer token.</param>
    /// <param name="flowName">The flow that authenticated the person.</param>
    /// <param name="at">When the person signed in.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success once the sign-in is recorded; otherwise the failure.</returns>
    Task<IGenericResult> Record(Guid userId, string? serviceName, string flowName, DateTimeOffset at, CancellationToken cancellationToken = default);

    /// <summary>Reads when each of the given people last signed in.</summary>
    /// <param name="userIds">The people.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The time of each person's latest sign-in. A person who has never signed in is absent from the result, which is the state
    /// "never signed in" and not a missing answer.
    /// </returns>
    Task<IGenericResult<IReadOnlyDictionary<Guid, DateTimeOffset>>> LastSignIns(IReadOnlyList<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>Reads a page of one person's sign-ins, newest first.</summary>
    /// <param name="userId">The person.</param>
    /// <param name="skip">How many of the newest sign-ins precede the page.</param>
    /// <param name="take">How many sign-ins the page holds at most.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The page, newest first; empty when the person has never signed in or the page is past the last one.</returns>
    Task<IGenericResult<IReadOnlyList<SignInRecord>>> SignIns(Guid userId, int skip, int take, CancellationToken cancellationToken = default);
}
