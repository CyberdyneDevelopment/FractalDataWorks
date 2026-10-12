namespace Fdw.Configuration;

/// <summary>
/// The contract every implementation configuration satisfies.
/// </summary>
/// <remarks>
/// Identity, name, and its own payload. The name, domain and implementation are the domain row's --
/// persisted there and nowhere else -- and are stamped here by the domain provider when it reads.
/// <para>
/// <see cref="Name"/> is settable but not persisted: there is one name for a configured member and
/// it lives on the domain row. The domain provider sets it here from the row it just read, which is
/// the only place both records are in hand.
/// </para>
/// </remarks>
public interface IImplementationConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the name, set by the domain provider from the domain row.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets the domain this implementation belongs to, from the domain row.</summary>
    /// <remarks>
    /// Read alongside <see cref="Name"/> and never persisted: the implementation table has no such
    /// column. It is here so a caller holding the implementation can act on it without reading the
    /// domain row a second time to find out what it is.
    /// </remarks>
    string Domain { get; set; }

    /// <summary>Gets or sets which implementation this is, from the domain row.</summary>
    /// <remarks>
    /// Never persisted: the implementation table has no such column. The domain row names its
    /// implementation, the domain provider dispatches on it, and stamps it here so a caller holding
    /// the implementation does not have to read the domain row again to know what it is.
    /// </remarks>
    string Implementation { get; set; }
}
