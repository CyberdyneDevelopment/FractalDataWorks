using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// A stored grant (FDW-6144): what runs may do as a person while the person is away, and until when. A schedule points at one, and so do the answer requests (FDW-6142).
/// </summary>
/// <remarks>
/// The grant says nothing about whether the person still may be acted as: that is judged when a run starts, as it is for every run that acts as a person. It says only what the person allowed and for how long.
/// </remarks>
public sealed class RunGrant
{
    /// <summary>Gets or sets the grant's id, the one key a consumer stores.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the person the runs act as: the caller who issued the grant.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Gets or sets when the grant ends.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets the permissions the grant allows: a run under it may hold no permission that is not here.</summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the person took the grant back.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Says whether the grant has ended: it was revoked, or the moment is not before its end.</summary>
    /// <param name="at">The moment to judge it at.</param>
    /// <returns><see langword="true"/> when the grant allows nothing at that moment.</returns>
    public bool HasEnded(DateTimeOffset at) => IsRevoked || at >= ExpiresAt;

    /// <summary>Gets the permissions among those given that the grant does not allow.</summary>
    /// <param name="wanted">The permissions a run would hold.</param>
    /// <returns>Each permission of <paramref name="wanted"/> that no permission of the grant covers (by the rule every permission check uses), once; none when the grant covers them all.</returns>
    public IReadOnlyList<string> NotAllowed(IEnumerable<string> wanted) =>
        [.. wanted.Distinct(StringComparer.OrdinalIgnoreCase).Where(permission => !PermissionCoverage.Covers(Permissions, permission))];
}
