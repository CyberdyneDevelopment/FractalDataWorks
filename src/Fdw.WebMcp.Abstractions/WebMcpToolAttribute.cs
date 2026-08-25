using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.WebMcp.Abstractions;

/// <summary>
/// Marks an endpoint option as a WebMCP tool. The endpoint it declares is offered to AI agents as a
/// <c>modelContext</c> tool in the generated <c>webmcp.js</c>.
/// </summary>
/// <remarks>
/// This goes on the ENDPOINT OPTION, not on the endpoint class, because the option is where an
/// endpoint already declares itself — <c>EndpointTypeOptionBase.Register</c> is the one place that
/// knows an endpoint is switched on, and an endpoint that is never declared is never routed. Marking
/// the endpoint class instead would offer agents tools for routes that return 404.
///
/// Carries no route. The route lives in the endpoint's FastEndpoints <c>Configure()</c> body, and a
/// copy here would be a second source of truth free to drift from the one the router actually uses.
/// It is resolved at <c>MapWebMcp</c> from the live endpoint data source instead.
/// </remarks>
// Why: pure attribute definition (declarative metadata only) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class WebMcpToolAttribute : Attribute
{
    /// <summary>Gets the tool name exposed to agents (snake_case, e.g. <c>list_connections</c>).</summary>
    public string Name { get; }

    /// <summary>Gets the human-readable description shown to AI agents.</summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets an HTTP method override.
    /// </summary>
    /// <remarks>
    /// Only needed when an endpoint maps more than one verb, where the live route table offers no
    /// single answer. Left unset the verb comes from the router, like the route.
    /// </remarks>
    public string? HttpMethod { get; init; }

    /// <summary>Gets or sets a value indicating whether the tool has no side effects.</summary>
    public bool ReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebMcpToolAttribute"/> class.
    /// </summary>
    /// <param name="name">Tool name exposed to AI agents (snake_case recommended).</param>
    /// <param name="description">Human-readable description shown to AI agents.</param>
    public WebMcpToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
