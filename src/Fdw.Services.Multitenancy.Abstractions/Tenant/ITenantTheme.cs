namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Represents theme configuration for a tenant.
/// </summary>
public interface ITenantTheme
{
    /// <summary>Gets the primary brand color (HSL: 'h s% l%').</summary>
    string PrimaryColor { get; }

    /// <summary>Gets the secondary brand color (HSL).</summary>
    string SecondaryColor { get; }

    /// <summary>Gets the accent brand color (HSL).</summary>
    string AccentColor { get; }

    /// <summary>Gets the background surface color (HSL).</summary>
    string BackgroundColor { get; }

    /// <summary>Gets the surface color (HSL).</summary>
    string SurfaceColor { get; }

    /// <summary>Gets the overlay surface color (HSL).</summary>
    string OverlayColor { get; }

    /// <summary>Gets the success feedback color (HSL).</summary>
    string SuccessColor { get; }

    /// <summary>Gets the warning feedback color (HSL).</summary>
    string WarningColor { get; }

    /// <summary>Gets the error feedback color (HSL).</summary>
    string ErrorColor { get; }

    /// <summary>Gets the info feedback color (HSL).</summary>
    string InfoColor { get; }

    /// <summary>Gets the main typography color (HSL).</summary>
    string TextMainColor { get; }

    /// <summary>Gets the muted typography color (HSL).</summary>
    string TextMutedColor { get; }

    /// <summary>Gets the logo URL.</summary>
    string? LogoUrl { get; }

    /// <summary>Gets the favicon URL.</summary>
    string? FaviconUrl { get; }

    /// <summary>Gets the custom CSS URL.</summary>
    string? CustomCssUrl { get; }

    /// <summary>Gets whether dark mode is enabled by default.</summary>
    bool DarkModeDefault { get; }
}
