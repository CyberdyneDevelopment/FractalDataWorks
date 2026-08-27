using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Holds the configuration gateways and hands back the one operating on a named connection.
/// </summary>
/// <remarks>
/// Configuration gateways differ only by the connection they read and write. A domain names the
/// connection its rows live on through its collection's <c>ConfigurationConnection</c> and asks here
/// for the gateway onto it.
/// <para>
/// Selection happens at resolve time rather than through a DI key because
/// <c>PlatformServices.&lt;Domain&gt;.ConfigurationConnectionName</c> is settable by a host; a key is
/// fixed when the container is built.
/// </para>
/// <para>
/// <see cref="Get"/> is synchronous, unlike every other domain provider: a configuration gateway is
/// not a row to be read, it is what reads rows.
/// </para>
/// </remarks>
public interface IConfigurationGatewayProvider
{
    /// <summary>Gets the gateway onto <paramref name="connectionName"/>.</summary>
    /// <param name="connectionName">The configuration connection, e.g. <c>PlatformConfiguration</c>.</param>
    /// <returns>The gateway, or a failure naming the connections that are registered.</returns>
    IGenericResult<IConfigurationGateway> Get(string connectionName);

    /// <summary>Adds <paramref name="gateway"/> under the connection it reports.</summary>
    /// <param name="gateway">The gateway to add.</param>
    /// <returns>Success, or a failure when the gateway names no connection or one is already held for it.</returns>
    /// <remarks>
    /// The connection is taken from <see cref="IConfigurationGateway.ConnectionName"/> rather than
    /// supplied alongside, so the name a gateway is filed under and the connection it actually opened
    /// can never disagree.
    /// </remarks>
    IGenericResult Register(IConfigurationGateway gateway);
}
