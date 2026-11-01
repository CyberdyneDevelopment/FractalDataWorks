namespace Fdw.Configuration;

using System;

/// <summary>
/// Base interface for every configuration record.
/// </summary>
/// <remarks>
/// Identity, and nothing else. A name belongs to a domain record; an implementation is identified by
/// its reference to the domain it configures, which is why <c>Name</c>, <c>Domain</c> and
/// <c>Implementation</c> all live on <see cref="IDomainConfiguration"/>.
/// <para>
/// <c>SectionName</c> and <c>ServiceType</c> used to live here and are gone. SectionName named an
/// appsettings section, and configuration has not come from appsettings since the gateway became the
/// one source; nothing read it but validators asserting the value they forced every class to invent.
/// </para>
/// </remarks>
public interface IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier for this configuration record.</summary>
    Guid Id { get; set; }
}

/// <summary>
/// Generic configuration interface for type-safe configuration.
/// </summary>
/// <typeparam name="T">The concrete configuration type.</typeparam>
public interface IGenericConfiguration<T> : IGenericConfiguration
    where T : IGenericConfiguration<T>
{
}
