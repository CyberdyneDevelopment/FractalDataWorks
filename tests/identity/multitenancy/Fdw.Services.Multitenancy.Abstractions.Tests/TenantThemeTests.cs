using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantThemeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SemanticColorsCanBeSet()
    {
        // Arrange
        var theme = ThemePalette.Stated();
        var customColor = "10 20% 30%";

        // Act
        theme.PrimaryColor = customColor;
        theme.SecondaryColor = customColor;
        theme.AccentColor = customColor;
        theme.BackgroundColor = customColor;
        theme.SurfaceColor = customColor;
        theme.OverlayColor = customColor;
        theme.SuccessColor = customColor;
        theme.WarningColor = customColor;
        theme.ErrorColor = customColor;
        theme.InfoColor = customColor;
        theme.TextMainColor = customColor;
        theme.TextMutedColor = customColor;

        // Assert
        theme.PrimaryColor.ShouldBe(customColor);
        theme.SecondaryColor.ShouldBe(customColor);
        theme.AccentColor.ShouldBe(customColor);
        theme.BackgroundColor.ShouldBe(customColor);
        theme.SurfaceColor.ShouldBe(customColor);
        theme.OverlayColor.ShouldBe(customColor);
        theme.SuccessColor.ShouldBe(customColor);
        theme.WarningColor.ShouldBe(customColor);
        theme.ErrorColor.ShouldBe(customColor);
        theme.InfoColor.ShouldBe(customColor);
        theme.TextMainColor.ShouldBe(customColor);
        theme.TextMutedColor.ShouldBe(customColor);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsITenantTheme()
    {
        // Act
        var result = ThemePalette.Stated();

        // Assert
        result.ShouldBeAssignableTo<ITenantTheme>();
    }
}