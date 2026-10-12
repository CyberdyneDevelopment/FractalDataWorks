using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Tds.Endpoints.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Tds.Endpoints.Hosting;

/// <summary>
/// Listens on the endpoint's address and serves each client that connects, as many at once as the options
/// allow.
/// </summary>
/// <remarks>
/// <para>
/// The options are read when the listener starts. Options that cannot be read stop the host: an endpoint
/// that is configured to listen and silently does not is the failure this refuses to hide.
/// </para>
/// <para>
/// A client that connects while every slot is taken is closed at once, before its handshake: the connection
/// limit bounds the work an unauthenticated stranger can make this server do.
/// </para>
/// <para>
/// Each connection is given a SPID from 51 up, where SQL Server numbers user sessions, so @@SPID and the
/// session id in every log line agree. Stopping the host stops accepting, cancels every connection and waits
/// for each to close.
/// </para>
/// </remarks>
public sealed class TdsListener : BackgroundService
{
    private const int FirstUserSessionId = 51;
    private const uint SessionIdSpan = (uint)(short.MaxValue - FirstUserSessionId);

    private readonly ITdsEndpointOptionsSource _source;
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<int, Task> _connections = new();
    private int _issued = -1;

    /// <summary>Initializes a new instance of the <see cref="TdsListener"/> class.</summary>
    /// <param name="source">Supplies the endpoint's options.</param>
    /// <param name="scopes">Creates the DI scopes connections run in.</param>
    /// <param name="logger">The logger.</param>
    public TdsListener(ITdsEndpointOptionsSource source, IServiceScopeFactory scopes, ILogger<TdsListener>? logger)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        _logger = logger ?? NullLogger<TdsListener>.Instance;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = await _source.Read(stoppingToken).ConfigureAwait(false);
        if (options.IsFailure)
        {
            var reason = TdsEndpointLog.ConfigurationInvalid(_logger, "SQL Server endpoint", "its options cannot be read");
            throw new InvalidOperationException(reason.Message);
        }

        using var slots = new SemaphoreSlim(options.Value!.MaximumConnections, options.Value.MaximumConnections);
        var listener = new TcpListener(options.Value.ListenOn);
        listener.Start();
        TdsEndpointLog.ListenerStarted(_logger, options.Value.ListenOn.ToString(), options.Value.Identity.ServerName);
        try
        {
            await Accept(listener, options.Value, slots, stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            listener.Dispose();
            await Task.WhenAll(_connections.Values).ConfigureAwait(false);
            TdsEndpointLog.ListenerStopped(_logger, options.Value.ListenOn.ToString());
        }
    }

    private async Task Accept(TcpListener listener, TdsEndpointOptions options, SemaphoreSlim slots, CancellationToken stoppingToken)
    {
        try
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken).ConfigureAwait(false);
                if (!await slots.WaitAsync(0, stoppingToken).ConfigureAwait(false))
                {
                    TdsEndpointLog.NotSupported(_logger, $"a connection from {client.Client.RemoteEndPoint} beyond the {options.MaximumConnections} allowed");
                    client.Dispose();
                    continue;
                }

                var sessionId = NextSessionId();
                _connections[sessionId] = Serve(client, sessionId, options, slots, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping; ExecuteAsync waits for the open connections to close.
        }
    }

    private int NextSessionId()
        => FirstUserSessionId + (int)((uint)Interlocked.Increment(ref _issued) % SessionIdSpan);

    // Every connection ends here, whatever ended it, so one client's failure is logged and never reaches the host.
    private async Task Serve(TcpClient client, int sessionId, TdsEndpointOptions options, SemaphoreSlim slots, CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            using (client)
            {
                client.NoDelay = true;
                TdsEndpointLog.ConnectionAccepted(_logger, $"{client.Client.RemoteEndPoint}", sessionId);
                await using var network = client.GetStream();
                var reason = await new TdsConnection(options, _scopes, _logger).Serve(network, sessionId, stoppingToken).ConfigureAwait(false);
                TdsEndpointLog.ConnectionClosed(_logger, sessionId, reason);
            }
        }
        catch (Exception ex)
        {
            TdsEndpointLog.ConnectionFaulted(_logger, ex, sessionId);
        }
        finally
        {
            _connections.TryRemove(sessionId, out _);
            slots.Release();
        }
    }
}
