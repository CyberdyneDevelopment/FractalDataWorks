using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.UI.Themes.Clients.Models;

/// <summary>
/// Represents a summary of a theme for listing purposes.
/// </summary>
// Why: pure payload, no logic.
[ExcludeFromCodeCoverage]
public sealed class ThemeSummaryPayload
{
    /// <summary>
    /// Gets or sets the unique identifier of the theme.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the tenant identifier. NULL for system-wide themes.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the unique name of the theme within a tenant scope.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the theme.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the description of the theme.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the primary theme color.
    /// </summary>
    public string PrimaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary theme color.
    /// </summary>
    public string SecondaryColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the default theme.
    /// </summary>
    public bool IsDefault { get; set; }
}
