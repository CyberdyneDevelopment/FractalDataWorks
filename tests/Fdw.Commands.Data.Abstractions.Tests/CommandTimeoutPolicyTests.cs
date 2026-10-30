using System;
using System.Collections.Generic;
using Fdw.Commands.Data.Abstractions.Timeouts;

namespace Fdw.Commands.Data.Abstractions.Tests;

/// <summary>
/// The timeout a data command carries in its metadata (FDW-3324): written with its owner, read back, and absent when the command carries none.
/// </summary>
public sealed class CommandTimeoutPolicyTests
{
    private static IDataCommand CommandWith(IReadOnlyDictionary<string, object> metadata)
    {
        var command = new Mock<IDataCommand>();
        command.Setup(c => c.Metadata).Returns(metadata);
        return command.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATimeoutAndItsOwnerAreReadBackFromTheMetadataTheyWereWrittenTo()
    {
        var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        CommandTimeoutPolicy.Set(metadata, TimeSpan.FromSeconds(45), "secret manager 'MsSqlSecrets'");

        CommandTimeoutPolicy.TryGet(CommandWith(metadata), out var timeout, out var owner).ShouldBeTrue();
        timeout.ShouldBe(TimeSpan.FromSeconds(45));
        owner.ShouldBe("secret manager 'MsSqlSecrets'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandThatCarriesNoTimeoutCarriesNone()
    {
        CommandTimeoutPolicy.TryGet(CommandWith(new Dictionary<string, object>()), out var timeout, out var owner).ShouldBeFalse();

        timeout.ShouldBe(TimeSpan.Zero);
        owner.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ACommandWithNoMetadataCarriesNoTimeout()
    {
        CommandTimeoutPolicy.TryGet(new Mock<IDataCommand>().Object, out _, out _).ShouldBeFalse();
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("0")]
    [InlineData("-5")]
    public void ATimeoutThatIsNotLongerThanZeroIsRefusedWhenWritten(string seconds)
    {
        var metadata = new Dictionary<string, object>();

        Should.Throw<ArgumentOutOfRangeException>(
            () => CommandTimeoutPolicy.Set(metadata, TimeSpan.FromSeconds(double.Parse(seconds, System.Globalization.CultureInfo.InvariantCulture)), "owner"));
        metadata.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnOwnerThatIsBlankIsRefusedWhenWritten()
    {
        Should.Throw<ArgumentException>(() => CommandTimeoutPolicy.Set(new Dictionary<string, object>(), TimeSpan.FromSeconds(1), " "));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AValueInTheMetadataThatIsNotATimeSpanIsNotATimeout()
    {
        var metadata = new Dictionary<string, object> { [CommandTimeoutPolicy.CommandTimeoutKey] = 30 };

        CommandTimeoutPolicy.TryGet(CommandWith(metadata), out _, out _).ShouldBeFalse();
    }
}
