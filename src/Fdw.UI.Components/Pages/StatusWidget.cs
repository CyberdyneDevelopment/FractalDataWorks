using System;
using Fdw.UI.Abstractions.Pages;

namespace Fdw.UI.Components.Pages;

/// <summary>
/// Concrete implementation of a status widget.
/// </summary>
public sealed class StatusWidget : IStatusWidget
{
    /// <inheritdoc />
    public string Id { get; set; } = "";

    /// <inheritdoc />
    public string Label { get; set; } = "";

    /// <inheritdoc />
    public IServiceStatus Status { get; set; } = ServiceStatuses.Unknown;

    /// <inheritdoc />
    public string? StatusMessage { get; set; }

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public DateTime? LastChecked { get; set; }

    /// <inheritdoc />
    public string? NavigationTarget { get; set; }

    /// <summary>
    /// Creates a healthy status widget.
    /// </summary>
    public static StatusWidget Healthy(string id, string label, string? message = null) =>
        new() { Id = id, Label = label, Status = ServiceStatuses.Healthy, StatusMessage = message ?? "Operational", LastChecked = DateTime.UtcNow };

    /// <summary>
    /// Creates an unhealthy status widget.
    /// </summary>
    public static StatusWidget Unhealthy(string id, string label, string message) =>
        new() { Id = id, Label = label, Status = ServiceStatuses.Unhealthy, StatusMessage = message, LastChecked = DateTime.UtcNow };

    /// <summary>
    /// Creates a degraded status widget.
    /// </summary>
    public static StatusWidget Degraded(string id, string label, string message) =>
        new() { Id = id, Label = label, Status = ServiceStatuses.Degraded, StatusMessage = message, LastChecked = DateTime.UtcNow };
}