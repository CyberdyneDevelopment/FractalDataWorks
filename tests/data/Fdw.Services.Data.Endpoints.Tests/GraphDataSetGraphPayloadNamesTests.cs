using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6241: the graph a caller reads carries the display name and the abbreviation the Graph create and update bodies take (FDW-6118), as the stored
/// value and nothing else: with none stored it is null, so a screen can tell a name that was cleared from one it left alone.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW6241")]
public sealed class GraphDataSetGraphPayloadNamesTests
{
    private static GraphDataSetConfiguration Stored(string? displayName, string? abbreviation) => new()
    {
        Name = "GasPrices", Description = "prices", DisplayName = displayName, Abbreviation = abbreviation,
    };

    private static GraphDataSetGraphPayload Opened(GraphDataSetConfiguration graph) => GraphDataSetGraphPayloads.Open(graph, DataSetQueryHelper.MapToDetail(graph));

    [Fact]
    public void AnOpenGraphCarriesTheStoredDisplayNameAndAbbreviation()
    {
        var payload = Opened(Stored("Gas prices", "GAS"));

        payload.DisplayName.ShouldBe("Gas prices");
        payload.Abbreviation.ShouldBe("GAS");
    }

    [Fact]
    public void AGraphWithNoneStoredAnswersNullForBothAndNotTheNameOrAnEmptyString()
    {
        var payload = Opened(Stored(null, null));

        payload.DisplayName.ShouldBeNull();
        payload.Abbreviation.ShouldBeNull();
    }

    [Fact]
    public void ARestrictedGraphCarriesThemToo()
    {
        var graph = Stored("Gas prices", "GAS");

        var payload = GraphDataSetGraphPayloads.Restricted(graph, DataSetQueryHelper.MapToDetail(graph), new GraphDataSetRestrictionPayload());

        payload.DisplayName.ShouldBe("Gas prices");
        payload.Abbreviation.ShouldBe("GAS");
    }
}
