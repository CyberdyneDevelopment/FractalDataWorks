using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.TestDouble;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Graphs.Tests;

/// <summary>
/// FDW-6240: build contributions are scoped and the Graph data set runner is held by singletons, so each read opens a scope of its own and takes the host's contributions
/// from it, as the host's start resolves them. A component type whose builder needs one builds in a Graph data set as it does in a Graph pipeline; a contribution that cannot be
/// built fails the read naming the data set; two reads never share an instance.
/// </summary>
public sealed class GraphDataSetContributionTests
{
    private static GraphDataSetConfiguration Graph()
    {
        var graph = new GraphDataSetConfiguration { Name = "Weather", Implementation = "Graph", FieldsOutComponent = "src", FieldsOutOutput = "output" };
        graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "NeededSource", DataSetName = "Readings", BatchSize = 2 });
        return graph;
    }

    private static IDataGateway ServingRows(int count)
    {
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows =
            [.. Enumerable.Range(0, count).Select(i => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?> { ["Id"] = i })];
        var gateway = new Mock<IDataGateway>();
        gateway
            .Setup(g => g.OpenRecordSource(It.IsAny<IDataCommand>(), It.IsAny<DataSetTarget>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(GenericResult<IRecordSource<DataRecord>>.Success((IRecordSource<DataRecord>)new RowListRecordSource(RowListRecordSource.SchemaOfRows(rows), rows))));
        return gateway.Object;
    }

    private static GraphDataSetRunner Runner(ServiceCollection services) =>
        new(new GraphDataSetReadOptions(10, 5, 2), services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), NullLogger<GraphDataSetRunner>.Instance);

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6240")]
    public async Task AComponentTypeThatNeedsAContributionReadsInAGraphDataSetWhenTheHostRegistersIt()
    {
        var services = new ServiceCollection();
        services.AddScoped<IFlowBuildContribution, LookupContribution>();

        var result = await Runner(services).Read(ServingRows(3), Graph(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value.ShouldNotBeNull().Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6240")]
    public async Task AComponentTypeThatNeedsAContributionTheHostDoesNotRegisterIsRefusedNamingTheDataSetTheTypeAndTheContribution()
    {
        var result = await Runner(new ServiceCollection()).Read(ServingRows(3), Graph(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRunFailedCode>();
        var details = result.Details.ShouldNotBeNull();
        details.GetValue<string>("dataSet").ShouldBe("Weather");
        var reason = details.GetValue<string>("reason").ShouldNotBeNull();
        reason.ShouldContain("NeededSource");
        reason.ShouldContain(nameof(LookupContribution));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6240")]
    public async Task AContributionThatCannotBeConstructedFailsTheReadNamingTheDataSetWithItsOwnMessage()
    {
        var services = new ServiceCollection();
        services.AddScoped<IFlowBuildContribution, NeedsAnUnregisteredService>();

        var result = await Runner(services).Read(ServingRows(3), Graph(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code.ShouldBeOfType<GraphDataSetRunFailedCode>();
        var details = result.Details.ShouldNotBeNull();
        details.GetValue<string>("dataSet").ShouldBe("Weather");
        details.GetValue<string>("reason").ShouldNotBeNull().ShouldContain(nameof(IUnregisteredService));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW6240")]
    public async Task TwoReadsGetTwoContributionInstances()
    {
        var services = new ServiceCollection();
        List<LookupContribution> created = [];
        services.AddScoped<IFlowBuildContribution>(_ =>
        {
            var contribution = new LookupContribution();
            created.Add(contribution);
            return contribution;
        });
        var runner = Runner(services);

        var first = await runner.Read(ServingRows(1), Graph(), TestContext.Current.CancellationToken);
        var second = await runner.Read(ServingRows(1), Graph(), TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue(first.CurrentMessage);
        second.IsSuccess.ShouldBeTrue(second.CurrentMessage);
        created.Count.ShouldBe(2);
        created[0].ShouldNotBeSameAs(created[1]);
        created.ShouldAllBe(contribution => contribution.Used);
    }

    private interface IUnregisteredService;

    private sealed class NeedsAnUnregisteredService : IFlowBuildContribution
    {
        public NeedsAnUnregisteredService(IUnregisteredService service)
        {
            Service = service;
        }

        public IUnregisteredService Service { get; }
    }
}
