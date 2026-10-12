using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Mcp.Bus.Abstractions;

/// <summary>
/// Bridges an MCP tool implementation onto an <see cref="IMcpEventBus"/>. Subscribes to
/// <c>mcp/{ServerName}/*/invoke</c> events and produces matching <c>.../result</c> or
/// <c>.../error</c> events on the same bus.
/// </summary>
/// <remarks>
/// <para>
/// Tool sources are how external MCP processes join an in-process bus. Three flavors are
/// expected: in-proc (the tool service is a library reference), stdio-bridge (the source spawns
/// an external MCP exe and bridges its JSON-RPC), and distributed-native (the tool publishes to
/// a cross-process bus directly — paired with a distributed <see cref="IMcpEventBus"/>).
/// </para>
/// <para>
/// A source is responsible for its own lifetime: <see cref="Start"/> wires up the subscription
/// (and spawns any child process); <see cref="Stop"/> cancels and tears down.
/// </para>
/// </remarks>
public interface IMcpToolSource : IAsyncDisposable
{
    /// <summary>MCP server name this source provides (e.g. <c>mssql</c>, <c>sqlmcp</c>).</summary>
    string ServerName { get; }

    /// <summary>Begin listening for invocations and producing responses.</summary>
    Task Start(IMcpEventBus bus, CancellationToken cancellationToken);

    /// <summary>Stop listening and tear down any owned resources.</summary>
    Task Stop(CancellationToken cancellationToken);
}
