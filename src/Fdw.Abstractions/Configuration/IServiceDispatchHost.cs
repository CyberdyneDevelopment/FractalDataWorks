namespace Fdw.Configuration;

/// <summary>
/// Implemented by a domain configuration whose implementation names a further implementation of its
/// own, and it is that deeper name — not the domain's — which selects the runtime service factory.
/// </summary>
/// <remarks>
/// Why: a domain provider resolves a record by name and reads
/// <see cref="IDomainConfiguration.Implementation"/> to pick the implementation. In most domains that
/// is the whole dispatch. In a layered one — Pipeline names EtlPipeline, EtlPipeline names BatchCopy —
/// factories are registered under the deeper name, so stopping at the domain's own discriminator
/// resolves the wrong factory. A domain that is layered says so by implementing this; one that is not
/// does not implement it, and dispatches on its own <see cref="IDomainConfiguration.Implementation"/>.
/// </remarks>
public interface IServiceDispatchHost
{
    /// <summary>
    /// Gets the implementation configuration that names the implementation selecting the runtime
    /// service factory, or <see langword="null"/> when this record does not delegate that choice.
    /// </summary>
    IGenericConfiguration? ServiceDispatchBody { get; }
}
