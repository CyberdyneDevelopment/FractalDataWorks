using System.Collections.Generic;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Provides access to the set of WebMCP tools discovered from decorated endpoints.
/// </summary>
public interface IWebMcpToolRegistry
{
    /// <summary>Gets the discovered tools, populated during application startup.</summary>
    IReadOnlyList<WebMcpToolDescriptor> Tools { get; }
}
