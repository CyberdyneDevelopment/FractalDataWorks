using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Results;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Resolves the account a <c>{IdOrName}</c> route segment names.
/// </summary>
/// <remarks>
/// One resolver, because every route scoped to an account has to answer the same three spellings:
/// a Guid id, a username, and <c>me</c> — the caller's own account. Leaving <c>me</c> to each
/// endpoint means a console has to know which routes accept it, which is exactly the kind of
/// difference nobody documents and everybody trips over.
/// </remarks>
public static class UserRouteTarget
{
    /// <summary>The route segment meaning "the calling account".</summary>
    public const string Self = "me";

    /// <summary>
    /// Reads the account the route names, resolving <c>me</c> against the caller's own subject.
    /// </summary>
    /// <param name="provider">Reads accounts.</param>
    /// <param name="caller">The calling principal.</param>
    /// <param name="idOrName">The route segment: a Guid id, a username, or <c>me</c>.</param>
    /// <param name="ct">Propagated cancellation token.</param>
    public static Task<IGenericResult<IUserImplementationConfiguration>> Resolve(
        UserConfigurationProvider provider, ClaimsPrincipal caller, string idOrName, CancellationToken ct)
    {
        if (string.Equals(idOrName, Self, StringComparison.OrdinalIgnoreCase))
        {
            var subject = caller.FindFirstValue(ClaimTypes.NameIdentifier) ?? caller.FindFirstValue("sub");
            return Guid.TryParse(subject, out var callerId)
                ? provider.Get(callerId, ct)
                : Task.FromResult(GenericResult<IUserImplementationConfiguration>.Failure(
                    UserResultCodes.ByName("UserNotFound")));
        }

        return Guid.TryParse(idOrName, out var requestedId)
            ? provider.Get(requestedId, ct)
            : provider.Get(idOrName, ct);
    }
}
