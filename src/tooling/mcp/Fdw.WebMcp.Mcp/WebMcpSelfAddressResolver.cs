using System;
using System.Threading;
using Fdw.Results;
using Fdw.WebMcp.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.WebMcp.Mcp;

/// <summary>
/// Gives the address the MCP bridge sends each tool call to: the one the source the WebMcp row names gives
/// (FDW-5721), worked out once the host is running and kept.
/// </summary>
/// <remarks>
/// Never worked out at Initialize: the <c>ListeningAddress</c> source reads the addresses Kestrel bound,
/// and Kestrel binds when the host starts. It is worked out when the host has started (where a failure
/// stops the host) and on a tool call, which can only arrive once the server has bound; whichever comes
/// first keeps the address. A failure is not kept, so it is reported each time it is asked.
/// </remarks>
public sealed class WebMcpSelfAddressResolver
{
    private readonly IServer _server;
    private readonly ILogger<WebMcpSelfAddressResolver> _logger;
    private Uri? _address;

    /// <summary>Initializes the resolver.</summary>
    /// <param name="server">The host's server, whose listening addresses the ListeningAddress source reads.</param>
    /// <param name="logger">The logger.</param>
    public WebMcpSelfAddressResolver(IServer server, ILogger<WebMcpSelfAddressResolver>? logger = null)
    {
        _server = server;
        _logger = logger ?? NullLogger<WebMcpSelfAddressResolver>.Instance;
    }

    /// <summary>Gives the host's own address from the settings' source.</summary>
    /// <param name="settings">The host's WebMCP settings.</param>
    /// <returns>The address, or the refusal naming why there is none.</returns>
    public IGenericResult<Uri> Resolve(WebMcpHostSettings settings)
    {
        if (Volatile.Read(ref _address) is { } known)
        {
            return GenericResult<Uri>.Success(known);
        }

        if (settings.SelfAddressSource is not { } source)
        {
            return GenericResult<Uri>.Failure(McpBridgeLog.SelfAddressSourceMissing(_logger));
        }

        var resolved = source.Resolve(settings, _server, _logger);
        if (resolved.IsSuccess && resolved.Value is { } address
            && Interlocked.CompareExchange(ref _address, address, null) is null)
        {
            McpBridgeLog.SelfAddressResolved(_logger, source.Name, address.ToString());
        }

        return resolved;
    }
}
