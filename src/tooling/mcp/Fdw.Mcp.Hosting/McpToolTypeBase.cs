using System;
using Fdw.Collections;

namespace Fdw.Mcp.Hosting;

/// <summary>Base class for <see cref="IMcpToolType"/> TypeOptions.</summary>
public abstract class McpToolTypeBase : TypeOptionBase<IMcpToolType>, IMcpToolType
{
    /// <summary>Required protected parameterless constructor for the TypeCollection NotFound sentinel.</summary>
    protected McpToolTypeBase() : base(0, "NotFound") { }

    /// <summary>Initializes an MCP tool-type option.</summary>
    /// <param name="name">Option name, used for lookup.</param>
    protected McpToolTypeBase(string name) : base(name) { }

    /// <inheritdoc />
    public abstract Type ToolClass { get; }
}
