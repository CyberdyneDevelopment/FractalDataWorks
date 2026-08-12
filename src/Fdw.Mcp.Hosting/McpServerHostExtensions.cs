using System;
using System.Collections.Generic;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Mcp.Hosting;

/// <summary>
/// Composes an MCP server from the tool packages the application references.
/// </summary>
/// <remarks>
/// <para>
/// This is the MCP counterpart to how an FDW API server is composed. An API server references
/// endpoint packages and FastEndpoints discovers their endpoints; an MCP server references tool
/// packages and this method discovers their tool classes through <see cref="McpToolTypes"/>,
/// which each package's module initializer populates at assembly load.
/// </para>
/// <para>
/// The entry-point application must reference <c>Fdw.Registration.SourceGenerators</c> for those
/// module initializers to be emitted; without it the collection is empty at runtime.
/// </para>
/// </remarks>
public static class McpServerHostExtensions
{
    /// <summary>
    /// Registers the MCP server and composes every tool class declared by the referenced tool
    /// packages onto it.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger.Instance"/>.</param>
    /// <returns>
    /// The configured builder on success, so the caller can chain a transport. A failure when no
    /// tool package is referenced — a server with no tools is a composition mistake, not a
    /// degraded-but-serviceable state, so it is reported rather than started empty.
    /// </returns>
    public static IGenericResult<IMcpServerBuilder> AddFdwMcpServer(
        this IServiceCollection services,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Why: NullLogger fallback is the one sanctioned ?? default — it keeps the method usable
        // before logging is wired without hiding a missing value.
        var resolvedLogger = logger ?? NullLogger.Instance;

        var toolClasses = new List<Type>();

        foreach (var toolType in McpToolTypes.All())
        {
            toolClasses.Add(toolType.ToolClass);
            McpHostingLog.ToolTypeRegistered(resolvedLogger, toolType.Name, toolType.ToolClass.FullName ?? toolType.ToolClass.Name);
        }

        if (toolClasses.Count == 0)
        {
            return GenericResult<IMcpServerBuilder>.Failure(McpHostingLog.NoToolTypesRegistered(resolvedLogger));
        }

        var builder = services.AddMcpServer();
        builder.WithTools(toolClasses, serializerOptions: null);

        McpHostingLog.CompositionComplete(resolvedLogger, toolClasses.Count);

        return GenericResult<IMcpServerBuilder>.Success(builder);
    }
}
