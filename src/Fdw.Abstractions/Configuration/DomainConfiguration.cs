using System;

namespace Fdw.Configuration;

/// <summary>
/// A domain record: the four properties every domain row has, and nothing else.
/// </summary>
/// <remarks>
/// Every domain row is the same shape, so this is a class and not a type parameter. It names a
/// configured member and the implementation that member is; the payload belongs to the
/// implementation. A property here would be one the domain has to have an opinion about for every
/// implementation of it, which is the thing the split exists to avoid.
/// </remarks>
public sealed class DomainConfiguration : IDomainConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name this record is resolved by.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the domain this record belongs to.</summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation this record names.</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the implementation's own configuration, attached by the domain provider.</summary>
    public IImplementationConfiguration? ImplementationConfiguration { get; set; }
}
