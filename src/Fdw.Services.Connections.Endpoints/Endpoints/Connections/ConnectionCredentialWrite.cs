using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// What storing a connection's credentials did: the secrets it wrote, and the secrets the connection no longer names
/// once it is saved (FDW-1086).
/// </summary>
/// <remarks>
/// Names only, never values. After a successful save the endpoint deletes <see cref="Retiring"/>; after a failed one it
/// deletes <see cref="Written"/>, which nothing names.
/// </remarks>
public sealed class ConnectionCredentialWrite
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionCredentialWrite"/> class.</summary>
    /// <param name="written">The secrets written for the connection.</param>
    /// <param name="retiring">The secrets the connection no longer names once saved.</param>
    public ConnectionCredentialWrite(IReadOnlyList<StoredSecret> written, IReadOnlyList<StoredSecret> retiring)
    {
        Written = written ?? throw new ArgumentNullException(nameof(written));
        Retiring = retiring ?? throw new ArgumentNullException(nameof(retiring));
    }

    /// <summary>Gets a write that stored nothing and retires nothing.</summary>
    public static ConnectionCredentialWrite Nothing { get; } = new([], []);

    /// <summary>Gets the secrets written for the connection.</summary>
    public IReadOnlyList<StoredSecret> Written { get; }

    /// <summary>Gets the secrets the connection no longer names once it is saved.</summary>
    public IReadOnlyList<StoredSecret> Retiring { get; }

    /// <summary>Returns this write with more secrets to retire after the save.</summary>
    /// <param name="more">The further secrets the connection no longer names.</param>
    /// <returns>A write whose <see cref="Retiring"/> also holds <paramref name="more"/>, without duplicates.</returns>
    public ConnectionCredentialWrite AlsoRetiring(IEnumerable<StoredSecret> more)
        => new(Written, Retiring.Concat(more ?? throw new ArgumentNullException(nameof(more))).Distinct().ToList());
}
