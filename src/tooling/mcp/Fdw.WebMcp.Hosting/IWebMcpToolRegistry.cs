using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Provides access to the set of WebMCP tools discovered from decorated endpoints.
/// </summary>
public interface IWebMcpToolRegistry
{
    /// <summary>Gets the discovered tools, populated during application startup.</summary>
    IReadOnlyList<WebMcpToolDescriptor> Tools { get; }

    /// <summary>
    /// Gets the plan of each discovered tool, in the order of <see cref="Tools"/>: what the agent is asked for and how a call
    /// becomes a request. Built once, when the tools are resolved, from the depth the setting <c>WebMcpToolSchemaMaxDepth</c>
    /// states, so every transport serves the one description and no transport needs the setting.
    /// </summary>
    IReadOnlyList<WebMcpToolPlan> Plans { get; }
}
