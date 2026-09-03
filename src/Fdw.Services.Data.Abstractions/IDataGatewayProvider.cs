using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Supplies the data gateway, resolved on the first ask rather than at construction.
/// </summary>
/// <remarks>
/// A configuration provider needs the gateway to read a row, and the gateway needs configuration to
/// know what it is reading -- so it cannot be handed over when the provider is built. This is the
/// seam that breaks that cycle: providers take this, ask once they are actually reading, and the
/// gateway is built by then.
///
/// It is the data-plane counterpart of <see cref="IConfigurationGatewayProvider"/>, and exists for
/// the same reason: services take providers, only providers take gateways.
/// </remarks>
public interface IDataGatewayProvider
{
    /// <summary>Gets the data gateway.</summary>
    /// <returns>The gateway, or a failure naming why none could be supplied.</returns>
    IGenericResult<IDataGateway> Get();
}
