using Fdw.Services.Multitenancy.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Multitenancy.Abstractions.Tests;

public class ThemeHelperTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToCssVariablesReturnsAll12Tokens()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToCssVariables();

        // Assert
        result.Count.ShouldBe(12);
        result["color-primary"].ShouldBe(theme.PrimaryColor);
        result["text-main"].ShouldBe(theme.TextMainColor);
        result.ContainsKey("color-accent").ShouldBeTrue();
        result.ContainsKey("color-bg").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToCssRootBlockGeneratesValidCss()
    {
        // Arrange
        var theme = new TenantTheme
        {
            PrimaryColor = "10 20% 30%"
        };

        // Act
        var result = theme.ToCssRootBlock();

        // Assert
        result.ShouldContain(":root {");
        result.ShouldContain("--color-primary: 10 20% 30%;");
        result.ShouldContain("}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToCssRootBlockUsesCustomSelector()
    {
        // Arrange
        var theme = new TenantTheme();

        // Act
        var result = theme.ToCssRootBlock(".custom-theme");

        // Assert
        result.ShouldContain(".custom-theme {");
    }
}
