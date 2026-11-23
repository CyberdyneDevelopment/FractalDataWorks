using System;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests.DataQuery;

/// <summary>
/// FDW-5446: a data route answers <c>dataSetName</c> under the name the caller asked for. A variation is read by running its root, so the data set that is read
/// is the root; a person who holds only the variation is told the variation's name and never the root's, whatever the root's kind.
/// </summary>
public sealed class DataQueryAnswerNameTests
{
    private sealed class Query(IDataSetResolver resolver, IDataGatewayProvider gateways)
        : QueryDataSetDataEndpointBase(NullLogger<QueryDataSetDataEndpointBase>.Instance, resolver, gateways);

    private sealed class Paging : IEffectiveSettingsProvider
    {
        public T GetEffectiveValue<T>(string settingName, Guid? tenantId = null, string? roleName = null)
            => TryGetEffectiveValue<T>(settingName, out var value, tenantId, roleName) ? value : throw new InvalidOperationException(settingName);

        public bool TryGetEffectiveValue<T>(string settingName, out T value, Guid? tenantId = null, string? roleName = null)
        {
            object? found = settingName switch
            {
                SettingDefinitions.MaxPaginationSize => 200,
                SettingDefinitions.DefaultPaginationSize => 50,
                SettingDefinitions.MaxCalculatedFilterRows => 1000,
                _ => null,
            };
            if (found is T typed)
            {
                value = typed;
                return true;
            }

            value = default!;
            return false;
        }
    }

    private static void WithPaging(DefaultHttpContext context)
        => context.RequestServices = new ServiceCollection().AddLogging().AddSingleton<IEffectiveSettingsProvider>(new Paging()).BuildServiceProvider();

    private static GraphDataSetConfiguration Graph()
    {
        var graph = new GraphDataSetConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "GasPrices",
            Implementation = "Graph",
            GraphFields = [new GraphDataSetFieldConfiguration { Id = Guid.NewGuid(), Name = "Name", TypeName = "String", Ordinal = 0, IsKey = true }],
        };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource" });
        return graph;
    }

    private static VariationDataSetConfiguration View(string name, Guid rootId) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        VariationName = name,
        Implementation = VariationDataSetConfiguration.ImplementationName,
        OwnerUserId = Guid.NewGuid(),
        BaseDataSetId = rootId,
    };

    private static async Task<DataQueryResponse> Ask(string asked, ResolvedDataSet resolved)
    {
        var resolver = new Mock<IDataSetResolver>();
        resolver
            .Setup(r => r.Resolve(asked, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ResolvedDataSet?>.Success(resolved));
        var gateway = DataSetQueryTests.Gateway([DataSetQueryTests.Row(("Name", "Ada"))]);
        var endpoint = Factory.Create<Query>(WithPaging, resolver.Object, DataSetQueryTests.Provider(gateway));

        await endpoint.HandleAsync(new DataQueryRequest { DataSetName = asked, Skip = 0, Take = 10 }, TestContext.Current.CancellationToken);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(200);
        return endpoint.Response;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AVariationOverASimpleRootAnswersUnderItsOwnNameAndNeverTheRoots()
    {
        var root = DataSetQueryTests.People();
        root.Id = Guid.NewGuid();
        var view = View("west-people", root.Id);

        var response = await Ask("west-people", new ResolvedDataSet(root, view, root.Name, []));

        response.DataSetName.ShouldBe("west-people");
        response.Rows.Count.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AVariationOverAGraphRootAnswersUnderItsOwnNameAndNeverTheGraphs()
    {
        var root = Graph();
        var view = View("west-gas", root.Id);

        var response = await Ask("west-gas", new ResolvedDataSet(root, view, root.Name, []));

        response.DataSetName.ShouldBe("west-gas");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task ADataSetThatIsNotAVariationAnswersUnderItsOwnNameAsBefore()
    {
        var people = DataSetQueryTests.People();

        var response = await Ask("People", new ResolvedDataSet(people, null, null, []));

        response.DataSetName.ShouldBe("People");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5446")]
    public async Task AStreamedAnswerIsWrittenUnderTheNameItWasToldAndOtherwiseTheDataSetsOwn()
    {
        var query = new DataSetQuery(DataSetQueryTests.Provider(DataSetQueryTests.Gateway([DataSetQueryTests.Row(("Name", "Ada"))])), NullLogger.Instance);

        var opened = await DataQueryStream.Open(
            query, DataSetQueryTests.People(), new DataQueryRequest { DataSetName = "People" }, 2, 100, DataSetQuery.Unscoped,
            NullLogger.Instance, TestContext.Current.CancellationToken);
        var stream = opened.Value.ShouldNotBeNull();

        stream.DataSetName.ShouldBe("People");
        stream.AnswerAs("west-people");
        stream.DataSetName.ShouldBe("west-people");
        new DataQueryStreamResponse(stream, 0).DataSetName.ShouldBe("west-people");
    }
}
