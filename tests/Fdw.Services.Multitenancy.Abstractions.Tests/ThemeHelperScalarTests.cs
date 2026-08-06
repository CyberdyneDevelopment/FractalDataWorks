using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

/// <summary>
/// Tests for ThemeHelper Scalar CSS bridge methods (FDW-18).
/// </summary>
public sealed class ThemeHelperScalarTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssVariablesReturnsCorrectVariableNames()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssVariables();

        // Assert
        result.ContainsKey("--scalar-color-1").ShouldBeTrue();
        result.ContainsKey("--scalar-color-2").ShouldBeTrue();
        result.ContainsKey("--scalar-color-3").ShouldBeTrue();
        result.ContainsKey("--scalar-color-accent").ShouldBeTrue();
        result.ContainsKey("--scalar-background-1").ShouldBeTrue();
        result.ContainsKey("--scalar-background-2").ShouldBeTrue();
        result.ContainsKey("--scalar-background-3").ShouldBeTrue();
        result.ContainsKey("--scalar-background-accent").ShouldBeTrue();
        result.ContainsKey("--scalar-color-green").ShouldBeTrue();
        result.ContainsKey("--scalar-color-red").ShouldBeTrue();
        result.ContainsKey("--scalar-color-yellow").ShouldBeTrue();
        result.ContainsKey("--scalar-color-blue").ShouldBeTrue();
        result.ContainsKey("--scalar-color-orange").ShouldBeTrue();
        result.ContainsKey("--scalar-border-color").ShouldBeTrue();
        result.ContainsKey("--scalar-button-1").ShouldBeTrue();
        result.ContainsKey("--scalar-button-1-hover").ShouldBeTrue();
        result.ContainsKey("--scalar-button-1-color").ShouldBeTrue();
        result.ContainsKey("--scalar-sidebar-background-1").ShouldBeTrue();
        result.ContainsKey("--scalar-sidebar-color-1").ShouldBeTrue();
        result.ContainsKey("--scalar-sidebar-color-2").ShouldBeTrue();
        result.ContainsKey("--scalar-sidebar-border-color").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssVariablesReturnsHexValues()
    {
        // Arrange
        var theme = new TenantTheme
        {
            PrimaryColor = "221 83% 53%"
        };

        // Act
        var result = theme.ToScalarCssVariables();

        // Assert
        var buttonColor = result["--scalar-button-1"];
        buttonColor.ShouldStartWith("#");
        buttonColor.Length.ShouldBe(7); // #rrggbb
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssVariablesBackgroundAccentHasAlphaSuffix()
    {
        // Arrange
        var theme = new TenantTheme
        {
            AccentColor = "262 83% 58%"
        };

        // Act
        var result = theme.ToScalarCssVariables();

        // Assert
        var bgAccent = result["--scalar-background-accent"];
        bgAccent.Length.ShouldBe(9); // #rrggbb1f (hex with alpha suffix)
        bgAccent.ShouldEndWith("1f");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssBlockDarkModeTargetsDarkModeSelector()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssBlock(darkMode: true);

        // Assert
        result.ShouldContain(".dark-mode {");
        result.ShouldNotContain(".light-mode");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssBlockLightModeTargetsLightModeSelector()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssBlock(darkMode: false);

        // Assert
        result.ShouldContain(".light-mode {");
        result.ShouldNotContain(".dark-mode");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssBlockNullDarkModeEmitsBothSelectors()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssBlock(darkMode: null);

        // Assert
        result.ShouldContain(".dark-mode {");
        result.ShouldContain(".light-mode {");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssBlockContainsScalarVariables()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssBlock(darkMode: true);

        // Assert
        result.ShouldContain("--scalar-color-1:");
        result.ShouldContain("--scalar-background-1:");
        result.ShouldContain("--scalar-button-1:");
        result.ShouldContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssBlockNullEmitsBothBlocksWithSameVariables()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssBlock(darkMode: null);

        // Assert
        // Both blocks should have the same variables
        var darkCount = CountOccurrences(result, "--scalar-color-1:");
        var lightCount = CountOccurrences(result, "--scalar-background-1:");
        darkCount.ShouldBe(2);
        lightCount.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void ToScalarCssBlockIsSuitableForAddHeadContentInjection()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToScalarCssBlock(darkMode: true);

        // Assert - Output should be valid CSS that can be embedded in a <style> tag
        result.ShouldNotBeNullOrEmpty();
        result.ShouldContain("{");
        result.ShouldContain("}");
        result.ShouldContain("--scalar-");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void HslToHexConvertsValidHslToHex()
    {
        // Arrange - "221 83% 53%" is a valid FDW HSL color (deep blue)
        var theme = new TenantTheme
        {
            PrimaryColor = "221 83% 53%"
        };

        // Act
        var result = theme.ToScalarCssVariables();
        var primaryHex = result["--scalar-button-1"];

        // Assert
        primaryHex.ShouldNotBeNullOrEmpty();
        primaryHex.ShouldStartWith("#");
        primaryHex.Length.ShouldBe(7);
        // The value should not be the fallback #000000
        primaryHex.ShouldNotBe("#000000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void HslToHexFallsBackToBlackOnEmptyInput()
    {
        // Arrange
        var theme = new TenantTheme
        {
            PrimaryColor = string.Empty
        };

        // Act
        var result = theme.ToScalarCssVariables();
        var primaryHex = result["--scalar-button-1"];

        // Assert
        primaryHex.ShouldBe("#000000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void HslToHexFallsBackToBlackOnInvalidInput()
    {
        // Arrange
        var theme = new TenantTheme
        {
            PrimaryColor = "not-a-color"
        };

        // Act
        var result = theme.ToScalarCssVariables();
        var primaryHex = result["--scalar-button-1"];

        // Assert
        primaryHex.ShouldBe("#000000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void HslToHexFallsBackToBlackWhenTooFewParts()
    {
        // Arrange - only hue and saturation, missing lightness
        var theme = new TenantTheme
        {
            PrimaryColor = "221 83%"
        };

        // Act
        var result = theme.ToScalarCssVariables();
        var primaryHex = result["--scalar-button-1"];

        // Assert
        primaryHex.ShouldBe("#000000");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public void HslToHexHandlesNullGracefully()
    {
        // Arrange - null color treated as empty
        var theme = new TenantTheme
        {
            PrimaryColor = null!
        };

        // Act
        var result = theme.ToScalarCssVariables();
        var primaryHex = result["--scalar-button-1"];

        // Assert
        primaryHex.ShouldBe("#000000");
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
