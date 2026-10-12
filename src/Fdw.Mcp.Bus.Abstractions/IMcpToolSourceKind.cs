using Fdw.Collections;

namespace Fdw.Mcp.Bus.Abstractions;

/// <summary>
/// TypeOption selector for how external MCP tool implementations are joined to the bus
/// (in-process library, stdio bridge to an external exe, distributed-native, ...).
/// Configuration picks a kind by name; the host registers an <see cref="IMcpToolSource"/>
/// instance per configured external server.
/// </summary>
public interface IMcpToolSourceKind : ITypeOption<int, IMcpToolSourceKind>
{
}
