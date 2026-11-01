using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Fdw.Protocols.Tds;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// How the SQL Server endpoint listens and signs callers in.
/// </summary>
/// <remarks>
/// Built once at startup from the host's configuration, by whatever host feature turns the endpoint on.
/// Every value is required; a host that cannot supply one does not start the endpoint.
/// </remarks>
public sealed class TdsEndpointOptions
{
    /// <summary>Initializes a new instance of the <see cref="TdsEndpointOptions"/> class.</summary>
    /// <param name="listenOn">The address and port to listen on.</param>
    /// <param name="certificate">The certificate that proves this server's name; it must hold its private key.</param>
    /// <param name="loginFlowName">The authentication flow a SQL login runs; it must deliver a session.</param>
    /// <param name="identity">The SQL Server this endpoint presents itself as.</param>
    /// <param name="maximumMessageBytes">The largest message a client may send.</param>
    /// <param name="maximumConnections">The most connections served at once.</param>
    /// <param name="loginTimeout">How long a new connection has to finish its handshake and login before it is closed.</param>
    public TdsEndpointOptions(
        IPEndPoint listenOn,
        X509Certificate2 certificate,
        string loginFlowName,
        TdsServerIdentity identity,
        int maximumMessageBytes,
        int maximumConnections,
        TimeSpan loginTimeout)
    {
        ListenOn = listenOn ?? throw new ArgumentNullException(nameof(listenOn));
        Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));
        LoginFlowName = loginFlowName ?? throw new ArgumentNullException(nameof(loginFlowName));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumMessageBytes, TdsMessageReader.SmallestLimit, nameof(maximumMessageBytes));
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumConnections, 1, nameof(maximumConnections));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(loginTimeout, TimeSpan.Zero, nameof(loginTimeout));
        MaximumMessageBytes = maximumMessageBytes;
        MaximumConnections = maximumConnections;
        LoginTimeout = loginTimeout;
    }

    /// <summary>Gets the address and port to listen on.</summary>
    public IPEndPoint ListenOn { get; }

    /// <summary>Gets the certificate that proves this server's name.</summary>
    public X509Certificate2 Certificate { get; }

    /// <summary>Gets the authentication flow a SQL login runs.</summary>
    public string LoginFlowName { get; }

    /// <summary>Gets the SQL Server this endpoint presents itself as.</summary>
    public TdsServerIdentity Identity { get; }

    /// <summary>Gets the largest message a client may send.</summary>
    public int MaximumMessageBytes { get; }

    /// <summary>Gets the most connections served at once.</summary>
    public int MaximumConnections { get; }

    /// <summary>
    /// Gets how long a new connection has to finish its handshake and login before it is closed, so a
    /// connection that opens and goes silent cannot hold one of the connection slots.
    /// </summary>
    public TimeSpan LoginTimeout { get; }
}
