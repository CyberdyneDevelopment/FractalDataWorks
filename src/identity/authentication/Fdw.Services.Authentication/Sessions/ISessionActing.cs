using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Sessions;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Authentication.Sessions;

/// <summary>
/// Acting with a role (FDW-2282): a person in a session starts a second session that holds only what one role grants, and the session
/// they were in sleeps until they return.
/// </summary>
/// <remarks>
/// The person does not become anyone else: the new session names the same person, narrowed to the role, with the role and the person
/// recorded on it as the <c>actingRole</c> and <c>actor</c> claims. What the role carries must be covered by what the session acted from
/// holds, except for the administrator of the platform, who may act with any role of the company they act in.
/// </remarks>
public interface ISessionActing
{
    /// <summary>Starts a session that acts with a role, and points the response's cookie at it.</summary>
    /// <param name="context">The request, authenticated by a session. That session goes to sleep.</param>
    /// <param name="roleName">The role to act with.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the refusal: no such role, a role of another company, or a role that carries what the session does not hold.</returns>
    Task<IGenericResult> Act(HttpContext context, string roleName, CancellationToken cancellationToken = default);

    /// <summary>Returns from acting: ends the acting sessions above a step, wakes that step, and points the response's cookie at it.</summary>
    /// <param name="context">The request, authenticated by an acting session.</param>
    /// <param name="step">The step to return to: 0 is the person's own session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the refusal: the session is not acting, or the step is not one below it.</returns>
    Task<IGenericResult> Return(HttpContext context, int step, CancellationToken cancellationToken = default);

    /// <summary>The sessions the person has stepped through, from their own to the one the request was made with.</summary>
    /// <param name="context">The request, authenticated by a session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The steps in order, step 0 first.</returns>
    Task<IGenericResult<IReadOnlyList<ActingStep>>> Steps(HttpContext context, CancellationToken cancellationToken = default);

    /// <summary>The roles the session may act with: those of the company it acts in that carry nothing it does not hold.</summary>
    /// <param name="context">The request, authenticated by a session.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The role names, for a picker. The platform administrator is offered every role of the company.</returns>
    Task<IGenericResult<IReadOnlyList<string>>> Roles(HttpContext context, CancellationToken cancellationToken = default);
}
