using System;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// The host's own address is the address the running process listens on (FDW-5721), so two instances of
/// one release on two ports (A/B slots) each call themselves, and the release carries no port.
/// </summary>
/// <remarks>
/// <para>
/// Read from the server's <see cref="IServerAddressesFeature"/>, which Kestrel fills with the addresses it
/// actually bound (a <c>--urls http://127.0.0.1:$PORT</c>, or port 0 made concrete) when it starts. Before
/// it starts the list is empty, so this is never read at Initialize: the MCP bridge reads it once the host
/// has started, and on a tool call, which can only arrive at a server that has bound.
/// </para>
/// <para>
/// Exactly one usable address is taken. A wildcard bind (0.0.0.0, [::], +, *) is not an address a host can
/// call, and more than one usable address is refused rather than one picked: the row's author listens on
/// one address or states <see cref="FixedAddressSource"/>.
/// </para>
/// </remarks>
[TypeOption(typeof(WebMcpSelfAddressSources), "ListeningAddress")]
public sealed class ListeningAddressSource : WebMcpSelfAddressSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="ListeningAddressSource"/> class.</summary>
    public ListeningAddressSource()
        : base("ListeningAddress", "Listening address", "The address the running process listens on, read from its server once it has bound")
    {
    }

    /// <inheritdoc/>
    protected override IGenericResult<Uri?> CheckRow(string configurationName, string? selfBaseAddress, ILogger logger)
        => NoStatedAddress(configurationName, selfBaseAddress, logger);

    /// <inheritdoc/>
    protected override IGenericResult<Uri> ResolveAddress(WebMcpHostSettings settings, IServer server, ILogger logger)
    {
        if (server.Features.Get<IServerAddressesFeature>() is not { } feature)
        {
            return GenericResult<Uri>.Failure(WebMcpLog.ServerAddressesUnavailable(logger, server.GetType().Name));
        }

        var usable = WebMcpListeningAddresses.Usable(feature.Addresses);
        if (usable.Length == 0)
        {
            return GenericResult<Uri>.Failure(WebMcpLog.NoUsableListeningAddress(logger, string.Join(", ", feature.Addresses)));
        }

        return usable.Length == 1
            ? GenericResult<Uri>.Success(usable[0])
            : GenericResult<Uri>.Failure(WebMcpLog.ListeningAddressAmbiguous(logger, string.Join(", ", usable.Select(static address => address.ToString()))));
    }
}
