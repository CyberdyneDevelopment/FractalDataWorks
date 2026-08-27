using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Supplies the <see cref="IConfigurationGateway"/> operating on a named configuration connection.
/// </summary>
/// <remarks>
/// Configuration gateways differ only by the connection they read and write. A domain names the
/// connection its rows live on through its collection's <c>ConfigurationConnection</c>, and asks here
/// for the gateway onto it.
/// <para>
/// Selection happens at resolve time rather than through a DI key because
/// <c>PlatformServices.&lt;Domain&gt;.ConfigurationConnectionName</c> is settable by a host; a key is
/// fixed when the container is built.
/// </para>
/// <para>
/// Get is synchronous, unlike every other domain provider: a configuration gateway is not a row to be
/// read, it is what reads rows.
/// </para>
/// </remarks>
public interface IConfigurationGatewayProvider
{
    /// <summary>Gets the gateway onto <paramref name="connectionName"/>.</summary>
    /// <param name="connectionName">The configuration connection, e.g. <c>PlatformConfiguration</c>.</param>
    IGenericResult<IConfigurationGateway> Get(string connectionName);

    /// <summary>Registers the gateway onto <paramref name="connectionName"/>.</summary>
    /// <param name="connectionName">The configuration connection the gateway operates on.</param>
    /// <param name="gateway">The gateway.</param>
    IGenericResult Register(string connectionName, IConfigurationGateway gateway);
}
