using System;
using System.Diagnostics;

namespace Fdw.ServiceManager.Models;

/// <summary>
/// Runtime status of a service.
/// </summary>
public sealed class ServiceStatus
{
    /// <summary>
    /// Gets the service definition.
    /// </summary>
    public required ServiceDefinition Definition { get; init; }

    /// <summary>
    /// Gets or sets whether the service is running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// Gets or sets the process ID if running.
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the process reference if running.
    /// </summary>
    public Process? Process { get; set; }

    /// <summary>
    /// Gets the display status text.
    /// </summary>
    public string StatusText => IsRunning ? "Running" : "Stopped";

    /// <summary>
    /// Gets the Spectre.Console markup for status.
    /// </summary>
    public string StatusMarkup => IsRunning
        ? "[green]● Running[/]"
        : "[dim]○ Stopped[/]";
}
