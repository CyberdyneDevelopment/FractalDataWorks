using System;

namespace Fdw.Configuration;

/// <summary>
/// The four properties every domain record has. A domain record has no others.
/// </summary>
/// <remarks>
/// A domain record names a configured member and the implementation that member is. It carries no
/// payload, because a property here is one the domain would have to have an opinion about for every
/// implementation of it -- and it holds no implementation, because it is never returned. It is the
/// lookup, not the answer.
/// <para>
/// Each domain still declares its own empty record deriving from this, because the mapper and the
/// command bind to a type and each domain reads its own table.
/// </para>
/// </remarks>
public abstract class DomainConfigurationBase : IDomainConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this record is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this record belongs to, read from the row.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation this record names.</summary>
    public string? Implementation { get; set; }
}
