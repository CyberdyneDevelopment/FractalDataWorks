using System;
using Fdw.Collections;

namespace Fdw.Mcp.Hosting;

/// <summary>Base class for <see cref="IMcpToolType"/> TypeOptions.</summary>
public abstract class McpToolTypeBase : TypeOptionBase<int, IMcpToolType>, IMcpToolType
{
    /// <summary>Required protected parameterless constructor for the TypeCollection NotFound sentinel.</summary>
    protected McpToolTypeBase() : base(0, "NotFound") { }

    /// <summary>Initializes an MCP tool-type option.</summary>
    /// <param name="id">Stable option id, unique within the collection.</param>
    /// <param name="name">Option name, used for lookup.</param>
    protected McpToolTypeBase(int id, string name) : base(id, name) { }

    /// <inheritdoc />
    public abstract Type ToolClass { get; }
}
