using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Marks a FastEndpoints endpoint for WebMCP tool registration.
/// The endpoint will appear as a navigator.modelContext tool in the generated webmcp.js.
/// </summary>
// Why: pure attribute definition (declarative metadata only, consumed by webmcp.js generation) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class WebMcpToolAttribute : Attribute
{
    /// <summary>Tool name (snake_case recommended, e.g. "list_connections").</summary>
    public string Name { get; }

    /// <summary>Human-readable description shown to AI agents.</summary>
    public string Description { get; }

    /// <summary>HTTP method override. Defaults to the endpoint's configured method.</summary>
    public string? HttpMethod { get; init; }

    /// <summary>Marks the tool as read-only (no side effects). Default: false.</summary>
    public bool ReadOnly { get; init; }

    /// <summary>
    /// Initializes a new instance of <see cref="WebMcpToolAttribute"/>.
    /// </summary>
    /// <param name="name">Tool name exposed to AI agents (snake_case recommended).</param>
    /// <param name="description">Human-readable description shown to AI agents.</param>
    public WebMcpToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
