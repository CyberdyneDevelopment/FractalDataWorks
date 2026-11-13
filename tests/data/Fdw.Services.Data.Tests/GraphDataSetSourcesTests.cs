using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Tests;

/// <summary>FDW-5299: what the stored data sets read is read whole for a Graph, so a data set only a stage names is an edge, and once for every other kind.</summary>
public sealed class GraphDataSetSourcesTests
{
    private static Mock<IDataSetConfigurationProvider> Provider()
    {
        var provider = new Mock<IDataSetConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(default!));
        return provider;
    }

    private static void Serve(Mock<IDataSetConfigurationProvider> provider, string name, int level, IDataSetImplementationConfiguration answer) =>
        provider
            .Setup(p => p.Get(name, level, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(answer));

    private static GraphDataSetSources Over(Mock<IDataSetConfigurationProvider> provider) =>
        new(provider.Object, new Mock<IDataStoreConfigurationProvider>().Object);

    private static GraphDataSetConfiguration Graph(string name, string? component, string? lookup)
    {
        var graph = new GraphDataSetConfiguration { Name = name };
        if (component is not null)
        {
            graph.Components.Add(new GraphDataSetComponentConfiguration { Name = "src", ComponentType = "DataSetSource", DataSetName = component });
        }

        if (lookup is not null)
        {
            graph.Components.Add(new GraphDataSetComponentConfiguration
            {
                Name = "look",
                ComponentType = "Lookup",
                Stages = [new GraphDataSetStageConfiguration { Lookups = [new GraphDataSetStageLookupConfiguration { LookupDataSet = lookup }] }],
            });
        }

        return graph;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AGraphIsReadWholeAndReadsTheDataSetsItsComponentsAndItsStageLookupsName()
    {
        var provider = Provider();
        Serve(provider, "Mid", ConfigurationLevels.Children, Graph("Mid", "Base", null));
        Serve(provider, "Mid", ConfigurationLevels.WholeAggregate, Graph("Mid", "Base", "Rates"));

        var facts = await Over(provider).DataSets(["Mid"], TestContext.Current.CancellationToken);

        facts.IsSuccess.ShouldBeTrue(facts.CurrentMessage);
        facts.Value!["Mid"].Reads.ShouldBe(["Base", "Rates"]);
        provider.Verify(p => p.Get("Mid", ConfigurationLevels.WholeAggregate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task AVariationAndACompoundAreReadOnceAndReadTheirRootAndTheirSources()
    {
        var provider = Provider();
        Serve(provider, "ByDivision", ConfigurationLevels.Children, new VariationDataSetConfiguration { Name = "ByDivision", SourceDataSetName = "Mid" });
        var compound = new CompoundDataSetConfiguration { Name = "Both" };
        compound.Sources.Add(new DataSetSourceConfiguration { SourceDataSetName = "Orders" });
        compound.Sources.Add(new DataSetSourceConfiguration { SourceDataSetName = "Customers" });
        Serve(provider, "Both", ConfigurationLevels.Children, compound);

        var facts = await Over(provider).DataSets(["ByDivision", "Both"], TestContext.Current.CancellationToken);

        facts.IsSuccess.ShouldBeTrue(facts.CurrentMessage);
        facts.Value!["ByDivision"].Reads.ShouldBe(["Mid"]);
        facts.Value["Both"].Reads.OrderBy(name => name, StringComparer.Ordinal).ShouldBe(["Customers", "Orders"]);
        provider.Verify(p => p.Get(It.IsAny<string>(), ConfigurationLevels.WholeAggregate, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataSets.FDW5299")]
    public async Task ADataSetThatDoesNotExistReadsNothing()
    {
        var facts = await Over(Provider()).DataSets(["Gone"], TestContext.Current.CancellationToken);

        facts.IsSuccess.ShouldBeTrue(facts.CurrentMessage);
        facts.Value!["Gone"].Exists.ShouldBeFalse();
        facts.Value["Gone"].Reads.ShouldBeEmpty();
    }
}
