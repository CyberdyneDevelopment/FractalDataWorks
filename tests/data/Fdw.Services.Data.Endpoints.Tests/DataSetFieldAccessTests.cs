using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>FDW-5505: a generic route reaches a data set's fields through what its kind states, so a kind that keeps its fields apart has its edits kept and what it has no rows for refused by name.</summary>
[Trait("Priority", "P0")]
[Trait("Category", "DataSets.FDW5505")]
public sealed class DataSetFieldAccessTests
{
    private static GraphDataSetConfiguration Graph()
    {
        var graph = new GraphDataSetConfiguration { Name = "Teams", Implementation = "Graph" };
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration
        {
            Id = Guid.NewGuid(), Name = "Name", FedByComponent = "src", FedByField = "Name", DatumTypeId = 1, IsFilterable = false, Ordinal = 0,
        });
        graph.GraphFields.Add(new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "City", FedByComponent = "src", FedByField = "City", DatumTypeId = 1, Ordinal = 1 });
        return graph;
    }

    private static SimpleDataSetConfiguration Simple()
    {
        var simple = new SimpleDataSetConfiguration { Name = "Orders", Implementation = "Simple" };
        simple.Fields.Add(new DataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Id", DatumTypeId = 1 });
        return simple;
    }

    [Fact]
    public void AKindThatKeepsTheSharedListIsEditedInPlaceAndNothingIsPutBack()
    {
        var simple = Simple();

        var fields = DataSetFieldAccess.Of(simple);

        ReferenceEquals(fields, simple.Fields).ShouldBeTrue();
        DataSetFieldAccess.Kept("Simple").ShouldBeNull();
    }

    [Fact]
    public void AKindThatKeepsItsFieldsApartStatesItAndHasNoHistoryOrStandIns()
    {
        var kept = DataSetFieldAccess.Kept("Graph").ShouldNotBeNull();

        kept.FieldsHaveHistory.ShouldBeFalse();
        kept.FieldsTakeStandIns.ShouldBeFalse();
    }

    [Fact]
    public void AnEditToTheFieldsOfAKindThatKeepsThemApartReachesItsOwnFieldsAndKeepsWhatFeedsThem()
    {
        var graph = Graph();
        var fields = DataSetFieldAccess.Of(graph);

        fields.Single(field => field.Name == "Name").DatumTypeId = 42;
        fields.Single(field => field.Name == "Name").IsFilterable = true;
        graph.GraphFields.Single(field => field.Name == "Name").DatumTypeId.ShouldBe(1);

        DataSetFieldAccess.Keep(graph, fields);

        var stored = graph.GraphFields.Single(field => field.Name == "Name");
        stored.DatumTypeId.ShouldBe(42);
        stored.IsFilterable.ShouldBeTrue();
        stored.FedByComponent.ShouldBe("src");
        stored.FedByField.ShouldBe("Name");
        graph.GraphFields.Single(field => field.Name == "City").DatumTypeId.ShouldBe(1);
    }

    [Fact]
    public void TheFieldsOfAKindThatKeepsThemApartKeepTheirIdsWhenPutBack()
    {
        var graph = Graph();
        var ids = graph.GraphFields.Select(field => field.Id).ToList();

        DataSetFieldAccess.Keep(graph, DataSetFieldAccess.Of(graph));

        graph.GraphFields.Select(field => field.Id).ShouldBe(ids);
    }

    [Fact]
    public void AHistoryOfAFieldOfAKindThatKeepsNoneIsRefusedByNameAndAnotherKindIsNot()
    {
        var refused = DataSetFieldAccess.AllowHistory(Graph(), "Name", NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Code.ShouldBeOfType<DataSetFieldHistoryNotKeptCode>();
        DataSetFieldAccess.AllowHistory(Simple(), "Id", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AStandInOnAFieldOfAKindThatDoesNotReadThemIsRefusedByNameAndAnotherKindIsNot()
    {
        var refused = DataSetFieldAccess.AllowStandIns(Graph(), "Name", NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Code.ShouldBeOfType<DataSetFieldStandInsNotTakenCode>();
        DataSetFieldAccess.AllowStandIns(Simple(), "Id", NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void AStandInRouteAboutAWholeDataSetIsRefusedWithTheCodeThatNamesNoField()
    {
        var refused = DataSetFieldAccess.AllowStandIns(Graph(), NullLogger.Instance);

        refused.IsFailure.ShouldBeTrue();
        refused.Code.ShouldBeOfType<DataSetStandInsNotTakenCode>();
        DataSetFieldAccess.AllowStandIns(Simple(), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    private static IDataSetConfigurationProvider Providing(IDataSetImplementationConfiguration dataSet)
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider.Setup(p => p.Get(dataSet.Name, It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(dataSet));
        return provider.Object;
    }

    [Fact]
    public async Task TheFieldResolverRefusesAUseTheKindHasNoRowsForBeforeItLooksForTheField()
    {
        var resolved = await DataSetFieldResolver.Resolve(
            Providing(Graph()), "Teams", "NoSuchField", DataSetFieldAccess.AllowHistory, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
        resolved.Code.ShouldBeOfType<DataSetFieldHistoryNotKeptCode>();
    }

    [Fact]
    public async Task TheFieldResolverStillFindsAFieldOfAKindThatKeepsTheSharedList()
    {
        var simple = Simple();

        var resolved = await DataSetFieldResolver.Resolve(
            Providing(simple), "Orders", "Id", DataSetFieldAccess.AllowStandIns, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        resolved.Value!.Value.Field.Name.ShouldBe("Id");
    }
}
