namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Default implementation of tenant theme.
/// </summary>
public sealed class TenantTheme : ITenantTheme
{
    /// <inheritdoc />
    public string PrimaryColor { get; set; } = "221 83% 53%"; // Default Blue

    /// <inheritdoc />
    public string SecondaryColor { get; set; } = "215 16% 47%";

    /// <inheritdoc />
    public string AccentColor { get; set; } = "262 83% 58%";

    /// <inheritdoc />
    public string BackgroundColor { get; set; } = "222 47% 11%";

    /// <inheritdoc />
    public string SurfaceColor { get; set; } = "217 33% 17%";

    /// <inheritdoc />
    public string OverlayColor { get; set; } = "215 28% 23%";

    /// <inheritdoc />
    public string SuccessColor { get; set; } = "142 71% 45%";

    /// <inheritdoc />
    public string WarningColor { get; set; } = "38 92% 50%";

    /// <inheritdoc />
    public string ErrorColor { get; set; } = "0 84% 60%";

    /// <inheritdoc />
    public string InfoColor { get; set; } = "199 89% 48%";

    /// <inheritdoc />
    public string TextMainColor { get; set; } = "210 40% 98%";

    /// <inheritdoc />
    public string TextMutedColor { get; set; } = "215 20% 65%";

    /// <inheritdoc />
    public string? LogoUrl { get; set; }

    /// <inheritdoc />
    public string? FaviconUrl { get; set; }

    /// <inheritdoc />
    public string? CustomCssUrl { get; set; }

    /// <inheritdoc />
    public bool DarkModeDefault { get; set; } = true;

    /// <summary>
    /// Gets the default theme.
    /// </summary>
    public static TenantTheme Default { get; } = new();
}
