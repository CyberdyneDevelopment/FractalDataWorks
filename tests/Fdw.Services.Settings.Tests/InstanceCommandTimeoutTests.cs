using System;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Settings.Tests;

/// <summary>
/// The command timeout of a connection or secret manager (FDW-3284): the number it states, else the instance's setting, and no number assumed
/// when neither is there.
/// </summary>
public sealed class InstanceCommandTimeoutTests
{
    private static Lazy<IEffectiveSettingsProvider> Instance(Mock<IEffectiveSettingsProvider> settings)
        => new(() => settings.Object);

    private static Mock<IEffectiveSettingsProvider> SettingHolds(int seconds)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = seconds;
        settings
            .Setup(s => s.TryGetEffectiveValue(SettingDefinitions.DefaultCommandTimeoutSeconds, out value, null, null))
            .Returns(true);
        return settings;
    }

    private static Mock<IEffectiveSettingsProvider> SettingAbsent()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        var value = 0;
        settings
            .Setup(s => s.TryGetEffectiveValue(SettingDefinitions.DefaultCommandTimeoutSeconds, out value, null, null))
            .Returns(false);
        return settings;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ResolveTakesTheStatedNumberAndNeverReadsTheSetting()
    {
        var settings = SettingHolds(99);
        var instance = Instance(settings);

        var result = InstanceCommandTimeout.Resolve(45, instance, "connection", "Main", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(45);
        instance.IsValueCreated.ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    [InlineData(0)]
    [InlineData(-5)]
    public void ResolveRefusesAStatedNumberThatIsNotPositiveNamingTheOwner(int stated)
    {
        var result = InstanceCommandTimeout.Resolve(stated, Instance(SettingHolds(30)), "connection", "Main", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SETTINGS-62032");
        result.Messages[^1].Message.ShouldContain("connection 'Main'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ResolveTakesTheSettingWhenNothingIsStated()
    {
        var result = InstanceCommandTimeout.Resolve(null, Instance(SettingHolds(60)), "secret manager", "Vault", NullLogger.Instance);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(60);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ResolveFailsNamingTheOwnerAndTheSettingWhenNothingIsStatedAndTheSettingIsAbsent()
    {
        var result = InstanceCommandTimeout.Resolve(null, Instance(SettingAbsent()), "connection", "Main", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SETTINGS-62030");
        result.Messages[^1].Message.ShouldContain("connection 'Main'");
        result.Messages[^1].Message.ShouldContain(SettingDefinitions.DefaultCommandTimeoutSeconds);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ResolveFailsWhenTheSettingIsNotPositive()
    {
        var result = InstanceCommandTimeout.Resolve(null, Instance(SettingHolds(0)), "connection", "Main", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SETTINGS-62030");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ResolveFailsNamingTheOwnerAndTheSettingWhenThereAreNoSettingsToRead()
    {
        var result = InstanceCommandTimeout.Resolve(null, null, "secret manager", "Vault", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SETTINGS-62031");
        result.Messages[^1].Message.ShouldContain("secret manager 'Vault'");
        result.Messages[^1].Message.ShouldContain(SettingDefinitions.DefaultCommandTimeoutSeconds);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void ResolveFailsNamingTheOwnerWhenTheSettingsAreReadThroughTheOwnerBeingBuilt()
    {
        // The value factory asks for its own value, as the settings provider does when it is built through the connection being built.
        var cell = new Lazy<IEffectiveSettingsProvider>[1];
        cell[0] = new Lazy<IEffectiveSettingsProvider>(() => cell[0].Value);

        var result = InstanceCommandTimeout.Resolve(null, cell[0], "connection", "PlatformConfiguration", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("SETTINGS-62033");
        result.Messages[^1].Message.ShouldContain("connection 'PlatformConfiguration'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void TheSettingIsDefinedAsAnInt32WithADescription()
    {
        SettingDefinitions.TryGet(SettingDefinitions.DefaultCommandTimeoutSeconds, out var definition).ShouldBeTrue();
        definition.DataType.ShouldBe("Int32");
        definition.Description.ShouldNotBeNullOrWhiteSpace();
    }
}
