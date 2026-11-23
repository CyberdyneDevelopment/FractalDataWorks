using Fdw.Data.DataSets.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6970: a create or update of a Graph data set may state the federation strategy its reads across data stores run under. A registered strategy is
/// kept, an empty one takes the stored one away so the FDW-level default applies, one left out keeps the stored one, and a name no strategy carries is
/// refused with the code a Federated data set's is.
/// </summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW6970")]
public sealed class GraphDataSetStrategyTests
{
    [Fact]
    public void ARegisteredStrategyIsKept()
    {
        var graph = new GraphDataSetConfiguration { Name = "OrderRefunds" };

        GraphDataSetDefinitions.Strategy(graph, "Sequential", NullLogger.Instance).IsSuccess.ShouldBeTrue();

        graph.FederationStrategy.ShouldBe("Sequential");
    }

    [Fact]
    public void AStrategyLeftOutKeepsTheStoredOne()
    {
        var graph = new GraphDataSetConfiguration { Name = "OrderRefunds", FederationStrategy = "Parallel" };

        GraphDataSetDefinitions.Strategy(graph, null, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        graph.FederationStrategy.ShouldBe("Parallel");
    }

    [Fact]
    public void AnEmptyStrategyTakesTheStoredOneAwaySoTheFdwLevelDefaultApplies()
    {
        var graph = new GraphDataSetConfiguration { Name = "OrderRefunds", FederationStrategy = "Parallel" };

        GraphDataSetDefinitions.Strategy(graph, string.Empty, NullLogger.Instance).IsSuccess.ShouldBeTrue();

        graph.FederationStrategy.ShouldBeNull();
    }

    [Fact]
    public void AStrategyNoMemberCarriesIsRefusedAndNothingIsSet()
    {
        var graph = new GraphDataSetConfiguration { Name = "OrderRefunds", FederationStrategy = "Parallel" };

        var result = GraphDataSetDefinitions.Strategy(graph, "Fastest", NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Fastest");
        graph.FederationStrategy.ShouldBe("Parallel");
    }
}
