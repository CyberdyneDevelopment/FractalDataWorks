namespace Fdw.Services.Multitenancy.Abstractions.Tests;

/// <summary>The palette these tests state for a tenant; the code carries no default palette (FDW-3823), so a test that needs colours says which.</summary>
internal static class ThemePalette
{
    /// <summary>Gets a theme that states all twelve colours and the dark-mode flag.</summary>
    /// <returns>The theme.</returns>
    internal static TenantTheme Stated() => new()
    {
        PrimaryColor = "221 83% 53%",
        SecondaryColor = "215 16% 47%",
        AccentColor = "262 83% 58%",
        BackgroundColor = "222 47% 11%",
        SurfaceColor = "217 33% 17%",
        OverlayColor = "215 28% 23%",
        SuccessColor = "142 71% 45%",
        WarningColor = "38 92% 50%",
        ErrorColor = "0 84% 60%",
        InfoColor = "199 89% 48%",
        TextMainColor = "210 40% 98%",
        TextMutedColor = "215 20% 65%",
        DarkModeDefault = true,
    };
}
