using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6118, FDW-6129: the Graph routes take a display name and an abbreviation as the other kinds' do (FDW-6117): a PATCH that states one sets it, one that leaves it out keeps
/// the stored one, and an empty one clears it. The length rule is DataSetNames.Check's, tested with FDW-6117.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "DataIntegrity")]
public sealed class GraphDataSetDisplayNameTests
{
    private static readonly string Kind = new GraphDataSetType().Name;

    private static GraphDataSetConfiguration Stored() => new() { Name = "GasPrices", Implementation = Kind, Version = "3.2", DisplayName = "Gas prices" };

    private static DataSetRequestValues Values() => new("GasPrices", NullLogger.Instance);

    [Fact]
    public void AStatedDisplayNameIsSetOnTheGraph()
    {
        var graph = Stored();

        var applied = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices", DisplayName = "Henry Hub gas prices" }, graph, null, Values());

        applied.IsSuccess.ShouldBeTrue();
        graph.DisplayName.ShouldBe("Henry Hub gas prices");
    }

    [Fact]
    public void AStatedAbbreviationIsSetOnTheGraph()
    {
        var graph = Stored();

        var applied = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices", Abbreviation = "HHGP" }, graph, null, Values());

        applied.IsSuccess.ShouldBeTrue();
        graph.Abbreviation.ShouldBe("HHGP");
        graph.DisplayName.ShouldBe("Gas prices");
    }

    [Fact]
    public void AnEmptyAbbreviationClearsTheStoredOne()
    {
        var graph = Stored();
        graph.Abbreviation = "GP";

        var applied = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices", Abbreviation = string.Empty }, graph, null, Values());

        applied.IsSuccess.ShouldBeTrue();
        graph.Abbreviation.ShouldBeNull();
    }

    [Fact]
    public void ADisplayNameLeftOutKeepsTheStoredOne()
    {
        var graph = Stored();

        var applied = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices" }, graph, null, Values());

        applied.IsSuccess.ShouldBeTrue();
        graph.DisplayName.ShouldBe("Gas prices");
    }

    [Fact]
    public void AnEmptyDisplayNameClearsTheStoredOne()
    {
        var graph = Stored();

        var applied = UpdateGraphDataSetEndpointBase.ApplyHeader(new UpdateGraphDataSetRequest { Name = "GasPrices", DisplayName = string.Empty }, graph, null, Values());

        applied.IsSuccess.ShouldBeTrue();
        graph.DisplayName.ShouldBeNull();
    }
}
