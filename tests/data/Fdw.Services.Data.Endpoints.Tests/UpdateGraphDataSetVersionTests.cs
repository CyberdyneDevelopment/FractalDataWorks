using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5509: a PATCH that states a blank version is judged as create judges one (the kind's declared version), a stated version is kept, and a version left out keeps the stored one.</summary>
public sealed class UpdateGraphDataSetVersionTests
{
    // Why the kind is made here: the declared version is read from the kind's registration, which happens when its assembly loads, so the test names the kind it relies on.
    private static readonly string Kind = new GraphDataSetType().Name;

    private static GraphDataSetConfiguration Stored() => new() { Name = "GasPrices", Implementation = Kind, Version = "3.2" };

    private static DataSetRequestValues Values() => new("GasPrices", NullLogger.Instance);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5509")]
    public void ABlankStatedVersionTakesTheKindsDeclaredVersionAsCreateDoes(string blank)
    {
        var graph = Stored();

        var result = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices", Version = blank }, graph, null, Values());

        result.IsSuccess.ShouldBeTrue();
        graph.Version.ShouldBe("1.0");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5509")]
    public void AStatedVersionIsKept()
    {
        var graph = Stored();

        var result = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices", Version = "2.1" }, graph, null, Values());

        result.IsSuccess.ShouldBeTrue();
        graph.Version.ShouldBe("2.1");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5509")]
    public void AVersionLeftOutKeepsTheStoredOne()
    {
        var graph = Stored();

        var result = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices" }, graph, null, Values());

        result.IsSuccess.ShouldBeTrue();
        graph.Version.ShouldBe("3.2");
    }
}
