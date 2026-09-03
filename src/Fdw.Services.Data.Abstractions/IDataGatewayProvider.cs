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
///
/// Named by lookup rather than a bare property so the shape matches every other provider's --
/// domain.Register(Name, implementation) elsewhere routes by the same ServiceOptionType a caller
/// asks for here. This framework ships one implementation, named "Main"; a caller names it rather
/// than the provider assuming it, so a second implementation is a routing change here, not a
/// reshaped interface.
/// </remarks>
public interface IDataGatewayProvider
{
    /// <summary>Gets the data gateway registered under the given name.</summary>
    /// <param name="name">The implementation's <c>ServiceOptionType</c> -- "Main" for the one this
    /// framework ships.</param>
    /// <returns>The gateway, or throws when none is registered under that name.</returns>
    IDataGateway ByName(string name);
}
