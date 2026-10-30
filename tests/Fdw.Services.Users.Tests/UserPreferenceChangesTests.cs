using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Users.Tests;

/// <summary>
/// What a preferences update may state (FDW-3061): a colour mode of light, dark or system, the legacy dark mode flag meaning dark or light, and a time
/// zone the server resolves. Anything refused changes nothing.
/// </summary>
public sealed class UserPreferenceChangesTests
{
    private static UserPreferencesImplementationConfiguration Stored() => new() { Language = "en", Timezone = "UTC" };

    [Theory]
    [InlineData("light", false)]
    [InlineData("dark", true)]
    [InlineData("system", false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AColorModeIsStoredWithItsLegacyDarkMirror(string colorMode, bool darkMirror)
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(new UpdateUserPreferencesRequest { ColorMode = colorMode }, stored, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        stored.ColorMode.ShouldBe(colorMode);
        stored.DarkMode.ShouldBe(darkMirror);
    }

    [Theory]
    [InlineData(true, "dark")]
    [InlineData(false, "light")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void TheLegacyDarkModeFlagAloneChoosesDarkOrLight(bool darkMode, string expected)
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(new UpdateUserPreferencesRequest { DarkMode = darkMode }, stored, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        stored.ColorMode.ShouldBe(expected);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AnUpdateThatStatesNoColorLeavesTheChoiceAsItWas()
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(new UpdateUserPreferencesRequest { Language = "fr" }, stored, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        stored.ColorMode.ShouldBeNull();
        stored.Language.ShouldBe("fr");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AColorModeThatIsNotOfferedIsRefusedAndNothingChanges()
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(
            new UpdateUserPreferencesRequest { ColorMode = "blue", Language = "fr" }, stored, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        stored.ColorMode.ShouldBeNull();
        stored.Language.ShouldBe("en");
    }

    [Theory]
    [InlineData("light", true)]
    [InlineData("system", true)]
    [InlineData("dark", false)]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AColorModeAndADarkModeFlagThatDisagreeAreRefused(string colorMode, bool darkMode)
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(
            new UpdateUserPreferencesRequest { ColorMode = colorMode, DarkMode = darkMode }, stored, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        stored.ColorMode.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AColorModeAndADarkModeFlagThatAgreeAreAccepted()
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(
            new UpdateUserPreferencesRequest { ColorMode = "dark", DarkMode = true }, stored, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        stored.ColorMode.ShouldBe("dark");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATimeZoneTheServerResolvesIsStored()
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(new UpdateUserPreferencesRequest { Timezone = "Etc/UTC" }, stored, NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        stored.Timezone.ShouldBe("Etc/UTC");
    }

    [Theory]
    [InlineData("Nowhere/Land")]
    [InlineData("")]
    [InlineData("  ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ATimeZoneTheServerDoesNotResolveIsRefusedAndNothingChanges(string timeZoneId)
    {
        var stored = Stored();

        var result = UserPreferenceChanges.Apply(
            new UpdateUserPreferencesRequest { Timezone = timeZoneId, ColorMode = "dark" }, stored, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        stored.Timezone.ShouldBe("UTC");
        stored.ColorMode.ShouldBeNull();
    }
}
