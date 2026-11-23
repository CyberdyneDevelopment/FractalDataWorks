using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// The FDW-level federation settings come through the one declared-defaults mechanism (FDW-6901): the value the host's configuration states
/// under <see cref="FederationSettingsConfiguration.Section"/> wins, else what the Federated kind and each strategy declare; a stated value that
/// cannot be used refuses registration and names its key.
/// </summary>
[Trait("Category", "DataSets.FDW6901")]
public sealed class FederationSettingsConfigurationTests
{
    private static HostApplicationBuilder Host(params (string Key, string Value)[] stated)
    {
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        foreach (var (key, value) in stated)
            builder.Configuration[$"{FederationSettingsConfiguration.Section}:{key}"] = value;
        return builder;
    }

    [Fact]
    public void WithNothingStatedTheDeclaredDefaultsApply()
    {
        var read = FederationSettingsConfiguration.Read(Host(), NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.DefaultStrategy.ShouldBe("Optimized", "the Federated kind declares it");
        read.Value.Strategies["Sequential"].ShouldBe(new FederationStrategySettings(false, 1, false));
        read.Value.Strategies["Parallel"].ShouldBe(new FederationStrategySettings(true, 4, false));
        read.Value.Strategies["Optimized"].ShouldBe(new FederationStrategySettings(true, 4, true));
    }

    [Fact]
    public void AStatedDefaultStrategyAndStrategyValuesWinOverTheDeclaredOnes()
    {
        var read = FederationSettingsConfiguration.Read(
            Host(
                ("DefaultStrategy", "Sequential"),
                ("Strategies:Parallel:MaxParallelReads", "8"),
                ("Strategies:Optimized:OrdersSources", "false")),
            NullLogger.Instance);

        read.IsSuccess.ShouldBeTrue(read.CurrentMessage);
        read.Value!.DefaultStrategy.ShouldBe("Sequential");
        read.Value.Strategies["Parallel"].ShouldBe(new FederationStrategySettings(true, 8, false));
        read.Value.Strategies["Optimized"].ShouldBe(new FederationStrategySettings(true, 4, false));
    }

    [Theory]
    [InlineData("DefaultStrategy", "Fastest", "DefaultStrategy")]
    [InlineData("Strategies:Fastest:MaxParallelReads", "2", "Strategies:Fastest")]
    [InlineData("Strategies:Parallel:MaxReads", "2", "Strategies:Parallel:MaxReads")]
    [InlineData("Strategies:Parallel:MaxParallelReads", "0", "Strategies:Parallel:MaxParallelReads")]
    [InlineData("Strategies:Parallel:MaxParallelReads", "many", "Strategies:Parallel:MaxParallelReads")]
    [InlineData("Strategies:Optimized:ReadsInParallel", "yes", "Strategies:Optimized:ReadsInParallel")]
    public void AStatedValueThatCannotBeUsedRefusesRegistrationNamingItsKey(string key, string value, string refusedKey)
    {
        var read = FederationSettingsConfiguration.Read(Host((key, value)), NullLogger.Instance);

        read.IsSuccess.ShouldBeFalse();
        read.CurrentMessage.ShouldNotBeNull().ShouldContain($"{FederationSettingsConfiguration.Section}:{refusedKey}", Case.Insensitive);
    }
}
