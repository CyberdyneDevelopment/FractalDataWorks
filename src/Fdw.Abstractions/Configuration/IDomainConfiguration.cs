namespace Fdw.Configuration;

/// <summary>
/// The non-generic face of a domain configuration: which implementation a configured member is, and
/// that implementation's configuration, without naming the domain's implementation contract.
/// </summary>
/// <remarks>
/// <see cref="IPlatformServiceConfiguration{TImplementationConfiguration}"/> is the typed form and
/// every domain configuration implements it. This base exists for the one caller that must read both
/// fields without knowing the domain — the platform service provider, which needs
/// <c>ServiceOptionType</c> to choose a factory and the implementation to hand it.
/// <para>
/// Both come from the domain row. The implementation table has no <c>ServiceOptionType</c> column,
/// because the discriminator is what selected that table; asking the implementation for it gets an
/// empty answer from every domain, which is exactly what happened.
/// </para>
/// </remarks>
public interface IDomainConfiguration : IGenericConfiguration
{
    /// <summary>Gets the implementation's own configuration, or null when it has not been composed.</summary>
    IGenericConfiguration? ImplementationConfiguration { get; }
}
