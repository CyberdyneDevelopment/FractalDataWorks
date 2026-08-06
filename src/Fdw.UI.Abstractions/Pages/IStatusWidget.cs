using System;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// A status widget showing the health of a service or component.
/// </summary>
public interface IStatusWidget
{
    /// <summary>
    /// Gets the widget identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the widget label.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the current status.
    /// </summary>
    IServiceStatus Status { get; }

    /// <summary>
    /// Gets the status message or details.
    /// </summary>
    string? StatusMessage { get; }

    /// <summary>
    /// Gets the icon for this widget.
    /// </summary>
    string? Icon { get; }

    /// <summary>
    /// Gets the timestamp of the last status check.
    /// </summary>
    DateTime? LastChecked { get; }

    /// <summary>
    /// Gets the navigation target when clicked (e.g., "connections/mssql").
    /// </summary>
    string? NavigationTarget { get; }
}