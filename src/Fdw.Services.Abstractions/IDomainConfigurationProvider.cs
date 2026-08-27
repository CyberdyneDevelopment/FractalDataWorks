using System.Collections.Generic;

namespace Fdw.Services.Abstractions;

/// <summary>
/// A configuration provider that knows the implementation configuration providers of its domain.
/// </summary>
/// <remarks>
/// A domain configuration provider names which implementation is configured; the implementation
/// configuration provider registered for that ServiceOptionType supplies that implementation's own
/// configuration. Both are needed to resolve a service, and an option registers its implementation
/// provider on the domain provider — so the domain provider is where the set already exists, and this
/// is how the service provider takes it rather than each option registering twice.
/// </remarks>
public interface IDomainConfigurationProvider
{
    /// <summary>Gets the implementation configuration providers, keyed by ServiceOptionType.</summary>
    IReadOnlyDictionary<string, IServiceConfigurationProvider> ImplementationProviders { get; }
}
