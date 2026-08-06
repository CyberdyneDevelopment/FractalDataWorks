namespace Fdw.Configuration;

/// <summary>
/// Implemented by a root-header configuration whose nested typed body carries the discriminator that
/// selects the runtime SERVICE factory — i.e. a multi-level (header → kind → engine) domain where the
/// header's own <see cref="IGenericConfiguration.ServiceOptionType"/> identifies the KIND but the
/// factory is registered under the deeper ENGINE discriminator.
/// </summary>
/// <remarks>
/// Why: the SERVICE provider resolves a config by NAME via the root header (which owns Name), but a
/// multi-level domain (e.g. Pipeline → EtlPipeline → BatchCopy) registers its factories under the
/// engine discriminator that lives on the nested typed body, not the header's kind. The provider drills
/// one level via this marker to pick the right factory; single-level domains do not implement it and
/// dispatch on their own <see cref="IGenericConfiguration.ServiceOptionType"/> as before.
/// </remarks>
public interface IServiceDispatchHost
{
    /// <summary>
    /// Gets the nested typed-body configuration whose <see cref="IGenericConfiguration.ServiceOptionType"/>
    /// selects the runtime service factory, or <see langword="null"/> when there is no nested body.
    /// </summary>
    IGenericConfiguration? ServiceDispatchBody { get; }
}
