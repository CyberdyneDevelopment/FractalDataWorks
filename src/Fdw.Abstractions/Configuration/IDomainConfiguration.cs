namespace Fdw.Configuration;

/// <summary>
/// A domain configuration: the record that names which implementation is configured and holds it.
/// </summary>
/// <remarks>
/// A domain record carries four things and nothing else: its identity, its name, the domain it is,
/// and the implementation it names. <see cref="Implementation"/> is the discriminator — read from
/// the row, never a constant compiled into a type — and it selects the implementation's own
/// configuration provider.
/// </remarks>
public interface IDomainConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the name this record is resolved by.</summary>
    string Name { get; set; }

    /// <summary>Gets or sets the domain this record belongs to, read from the domain row.</summary>
    string Domain { get; set; }

    /// <summary>
    /// Gets or sets the implementation this record names — "MsSql", "Serilog", "Host".
    /// Read from the domain row.
    /// </summary>
    string? Implementation { get; set; }

    /// <summary>Gets or sets the implementation's own configuration, or null when it has not been composed.</summary>
    /// <remarks>
    /// Settable because the domain provider is what fills it: it reads the row, resolves the provider
    /// the row's <see cref="Implementation"/> names, and attaches what comes back. A get-only view
    /// forced every domain record to hand-write a constructor the provider could call instead, which
    /// is 28 identical bodies doing what one assignment does.
    /// </remarks>
    IImplementationConfiguration? ImplementationConfiguration { get; set; }
}
