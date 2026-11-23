using System;
using System.Collections.Generic;
using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>
/// A federated read's strategy and options are resolved in one place (FDW-6901): the data set's own value, else the FDW-level setting, else
/// the value the strategy declares; each is reported with where it came from, and a name or value that does not fit is refused.
/// </summary>
public sealed class FederationResolutionTests
{
    private static FederationSettings Settings(string strategy, FederationStrategySettings? options = null) =>
        new("Optimized", options is null
            ? new Dictionary<string, FederationStrategySettings>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, FederationStrategySettings>(StringComparer.OrdinalIgnoreCase) { [strategy] = options });

    private static DataSetFederationOptionConfiguration Option(string name, string value) => new() { OptionName = name, OptionValue = value };

    [Fact]
    [Trait("Category", "DataSets.FDW6901")]
    public void WithNothingStatedTheDefaultStrategyAndItsDeclaredValuesApply()
    {
        var resolved = FederationResolution.Resolve(Settings("Optimized"), "Orders", null, [], NullLogger.Instance);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value!.Strategy.ShouldBe("Optimized");
        resolved.Value.StrategyFrom.ShouldBe(FederationValueSources.Configuration);
        resolved.Value.ReadsInParallel.ShouldBeTrue();
        resolved.Value.ReadsInParallelFrom.ShouldBe(FederationValueSources.Declared);
        resolved.Value.MaxParallelReads.ShouldBe(4);
        resolved.Value.OrdersSources.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6901")]
    public void TheFdwLevelValueWinsOverTheDeclaredOneAndTheDataSetsOverBoth()
    {
        var settings = Settings("Parallel", new FederationStrategySettings(ReadsInParallel: null, MaxParallelReads: 2, OrdersSources: true));

        var fdw = FederationResolution.Resolve(settings, "Orders", "Parallel", [], NullLogger.Instance);
        var own = FederationResolution.Resolve(settings, "Orders", "Parallel", [Option("MaxParallelReads", "7"), Option("OrdersSources", "false")], NullLogger.Instance);

        fdw.Value!.MaxParallelReads.ShouldBe(2);
        fdw.Value.MaxParallelReadsFrom.ShouldBe(FederationValueSources.Configuration);
        fdw.Value.OrdersSources.ShouldBeTrue();
        own.Value!.MaxParallelReads.ShouldBe(7);
        own.Value.MaxParallelReadsFrom.ShouldBe(FederationValueSources.DataSet);
        own.Value.OrdersSources.ShouldBeFalse();
        own.Value.StrategyFrom.ShouldBe(FederationValueSources.DataSet);
    }

    [Theory]
    [Trait("Category", "DataSets.FDW6901")]
    [InlineData("Speed", "high", "Speed")]
    [InlineData("MaxParallelReads", "0", "a whole number of at least one")]
    [InlineData("MaxParallelReads", "many", "a whole number of at least one")]
    [InlineData("ReadsInParallel", "maybe", "true or false")]
    public void AnOptionTheStrategyDoesNotHaveOrAValueThatDoesNotParseIsRefused(string name, string value, string named)
    {
        var resolved = FederationResolution.Resolve(Settings("Parallel"), "Orders", "Parallel", [Option(name, value)], NullLogger.Instance);

        resolved.IsFailure.ShouldBeTrue();
        resolved.CurrentMessage.ShouldNotBeNull().ShouldContain(named);
    }

    [Fact]
    [Trait("Category", "DataSets.FDW6901")]
    public void NoStatedStrategyAndNoSettingsIsRefused()
        => FederationResolution.Resolve(null, "Orders", null, [], NullLogger.Instance).IsFailure.ShouldBeTrue();

    [Theory]
    [Trait("Category", "DataSets.FDW6901")]
    [InlineData("Speed", "high")]
    [InlineData("MaxParallelReads", "0")]
    [InlineData("OrdersSources", "sometimes")]
    public void TheSaveCheckRefusesWhatAReadWouldRefuse(string name, string value)
        => FederationResolution.Check("Orders", [Option(name, value)], NullLogger.Instance).IsFailure.ShouldBeTrue();

    [Fact]
    [Trait("Category", "DataSets.FDW6901")]
    public void TheSaveCheckRefusesAnOptionStatedTwice()
        => FederationResolution.Check("Orders", [Option("OrdersSources", "true"), Option("OrdersSources", "false")], NullLogger.Instance).IsFailure.ShouldBeTrue();

    [Fact]
    [Trait("Category", "DataSets.FDW6901")]
    public void TheSaveCheckAcceptsWellFormedValues()
        => FederationResolution.Check("Orders", [Option("ReadsInParallel", "true"), Option("MaxParallelReads", "3")], NullLogger.Instance).IsSuccess.ShouldBeTrue();
}
