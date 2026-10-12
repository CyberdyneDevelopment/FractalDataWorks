using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class TenantThemeTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorSetsDefaultHSLColors()
    {
        // Act
        var result = new TenantTheme();

        // Assert
        result.PrimaryColor.ShouldBe("221 83% 53%");
        result.SecondaryColor.ShouldBe("215 16% 47%");
        result.AccentColor.ShouldBe("262 83% 58%");
        result.BackgroundColor.ShouldBe("222 47% 11%");
        result.SurfaceColor.ShouldBe("217 33% 17%");
        result.OverlayColor.ShouldBe("215 28% 23%");
        result.SuccessColor.ShouldBe("142 71% 45%");
        result.WarningColor.ShouldBe("38 92% 50%");
        result.ErrorColor.ShouldBe("0 84% 60%");
        result.InfoColor.ShouldBe("199 89% 48%");
        result.TextMainColor.ShouldBe("210 40% 98%");
        result.TextMutedColor.ShouldBe("215 20% 65%");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void LogoUrlDefaultsToNull()
    {
        // Act
        var result = new TenantTheme();

        // Assert
        result.LogoUrl.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DarkModeDefaultDefaultsToTrue()
    {
        // Act
        var result = new TenantTheme();

        // Assert
        result.DarkModeDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SemanticColorsCanBeSet()
    {
        // Arrange
        var theme = new TenantTheme();
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
    public void DefaultReturnsSharedInstance()
    {
        // Act
        var first = TenantTheme.Default;
        var second = TenantTheme.Default;

        // Assert
        first.ShouldBeSameAs(second);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ImplementsITenantTheme()
    {
        // Act
        var result = new TenantTheme();

        // Assert
        result.ShouldBeAssignableTo<ITenantTheme>();
    }
}