using Fdw.Results;
using Fdw.UI.Themes.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.UI.Themes.Tests;

/// <summary>
/// A theme row that leaves out a text color is refused with a coded message naming the theme and the color, never thrown as an
/// uncoded exception that GET /themes/default answers as a 500 (FDW-6479).
/// </summary>
public sealed class ThemeConfigurationMapperTextColorTests
{
    private static ThemeImplementationConfiguration Theme(string? textOnPrimary, string? textOnSecondary) => new()
    {
        Name = "Default",
        PrimaryColor = "#7209B7",
        SecondaryColor = "#F72585",
        BackgroundColor = "#0F1115",
        TextOnPrimary = textOnPrimary,
        TextOnSecondary = textOnSecondary
    };

    [Fact]
    [Trait("Category", "Themes.FDW6479")]
    public void AThemeWithoutTextOnPrimaryIsRefusedWithACodedMessage()
    {
        var refused = Theme(null, "#0F1115").ToDto(NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.FailureReason().Code.ShouldBe("THEMES-63100");
        refused.FailureReason().Message.ShouldContain("Default");
        refused.FailureReason().Message.ShouldContain("TextOnPrimary");
    }

    [Fact]
    [Trait("Category", "Themes.FDW6479")]
    public void AThemeWithoutTextOnSecondaryIsRefusedWithACodedMessage()
    {
        var refused = Theme("#FFFFFF", null).ToDto(NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.FailureReason().Code.ShouldBe("THEMES-63100");
        refused.FailureReason().Message.ShouldContain("Default");
        refused.FailureReason().Message.ShouldContain("TextOnSecondary");
    }

    [Fact]
    [Trait("Category", "Themes.FDW6479")]
    public void AThemeThatStatesBothTextColorsIsMappedAsStated()
    {
        var mapped = Theme("#FFFFFF", "#0F1115").ToDto(NullLogger.Instance);

        mapped.IsSuccess.ShouldBeTrue(mapped.CurrentMessage);
        mapped.Value!.TextOnPrimary.ShouldBe("#FFFFFF");
        mapped.Value.TextOnSecondary.ShouldBe("#0F1115");
    }
}
