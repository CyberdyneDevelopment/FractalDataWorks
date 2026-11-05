namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Default implementation of tenant theme.
/// </summary>
public sealed class TenantTheme : ITenantTheme
{
    /// <inheritdoc />
    public string PrimaryColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SecondaryColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string AccentColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string BackgroundColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SurfaceColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string OverlayColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SuccessColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string WarningColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string ErrorColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string InfoColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string TextMainColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string TextMutedColor { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? LogoUrl { get; set; }

    /// <inheritdoc />
    public string? FaviconUrl { get; set; }

    /// <inheritdoc />
    public string? CustomCssUrl { get; set; }

    /// <inheritdoc />
    public bool DarkModeDefault { get; set; }
}
