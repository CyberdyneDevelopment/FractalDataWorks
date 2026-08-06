using Fdw.Collections;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>Base class for <see cref="IMcpToolSourceKind"/> TypeOptions.</summary>
public abstract class McpToolSourceKindBase : TypeOptionBase<int, IMcpToolSourceKind>, IMcpToolSourceKind
{
    /// <summary>Required protected parameterless constructor for the TypeCollection Empty sentinel.</summary>
    protected McpToolSourceKindBase() : base(0, "NotFound") { }

    /// <summary>Initializes a tool-source kind option.</summary>
    protected McpToolSourceKindBase(int id, string name) : base(id, name) { }
}
