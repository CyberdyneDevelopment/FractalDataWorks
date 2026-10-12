using System;
using System.Collections.Generic;

namespace Fdw.ServiceManager.Models;

/// <summary>
/// Defines a service that can be managed by the service manager.
/// </summary>
public sealed class ServiceDefinition
{
    /// <summary>
    /// Gets or sets the service key (e.g., "1", "2", etc.).
    /// </summary>
    public required string Key { get; init; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the HTTPS port.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Gets or sets the solution path relative to ReferenceSolutions.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets or sets the project path relative to the solution (null for Node.js).
    /// </summary>
    public string? Project { get; init; }

    /// <summary>
    /// Gets or sets the runtime type ("dotnet" or "node").
    /// </summary>
    public string Runtime { get; init; } = "dotnet";

    /// <summary>
    /// Gets or sets the keys of required dependencies.
    /// </summary>
    public IReadOnlyList<string> Requires { get; init; } = [];

    /// <summary>
    /// Gets or sets the keys of optional dependencies.
    /// </summary>
    public IReadOnlyList<string> Optional { get; init; } = [];
}
