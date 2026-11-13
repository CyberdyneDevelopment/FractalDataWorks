using System;
using System.Security.Claims;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Who is making a change to a Graph data set and when (FDW-5452): the author and time recorded on every new note and intended source.</summary>
/// <remarks>
/// A change that saves must name its caller; one that cannot is refused, never stored without an author. A check (the validate route) saves nothing, so it
/// records no author and time and resolves no id against what is stored.
/// </remarks>
/// <param name="Caller">The user id of the caller; null when the caller cannot be named.</param>
/// <param name="Now">When the change is made.</param>
/// <param name="Saves">Whether the change is saved: false for a check.</param>
internal sealed record GraphDataSetStamp(Guid? Caller, DateTimeOffset Now, bool Saves)
{
    /// <summary>Gets the stamp of a check: nothing is saved, so nothing is stamped.</summary>
    internal static GraphDataSetStamp Checking { get; } = new(null, DateTimeOffset.UtcNow, false);

    /// <summary>Makes the stamp of a change that is saved.</summary>
    /// <param name="caller">The user id of the caller; null when the caller cannot be named.</param>
    internal static GraphDataSetStamp Saving(Guid? caller) => new(caller, DateTimeOffset.UtcNow, true);

    /// <summary>Makes the stamp of a change that is saved by the person the principal names; a principal that names no user id has no caller.</summary>
    /// <param name="user">The signed-in caller.</param>
    internal static GraphDataSetStamp Saving(ClaimsPrincipal user) =>
        Saving(Guid.TryParse(new ClaimsPrincipalAuthenticationContext(user).UserId, out var id) ? id : null);

    /// <summary>Names the author of what a saved change makes new; a check names none, and a caller that cannot be named is refused.</summary>
    /// <param name="dataSet">The name of the Graph data set.</param>
    /// <param name="logger">The logger.</param>
    internal IGenericResult<Guid?> Author(string dataSet, ILogger logger) => !Saves || Caller is not null
        ? GenericResult<Guid?>.Success(Caller)
        : GenericResult<Guid?>.Failure(DataServiceResultCodes.ByName("GraphDataSetAuthorUnresolved"), logger, ResultDetails.Create("dataSet", dataSet));
}
