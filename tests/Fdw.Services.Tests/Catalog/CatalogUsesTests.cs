using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Catalog;

/// <summary>
/// FDW-1683: where a named thing is used and what it uses, read from the pipelines, calculations, terms and data sets themselves. The
/// providers are doubles, so what is tested is what is read from each, the direction, the parents and the failure of a read.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class CatalogUsesTests
{
    private static GraphPipelineConfiguration Pipeline(string name, params PipelineComponentConfiguration[] components) => new()
    {
        Name = name,
        Components = [.. components],
    };

    private static PipelineComponentConfiguration Source(string dataSet) => new() { Name = "src", ComponentType = "DataSetSource", DataSetName = dataSet };

    private static PipelineComponentConfiguration Sink(string dataSet) => new() { Name = "out", ComponentType = "DataSetDestination", DataSetName = dataSet };

    private static Mock<IPipelineConfigurationProvider> Pipelines(params GraphPipelineConfiguration[] graphs)
    {
        var provider = new Mock<IPipelineConfigurationProvider>();
        IReadOnlyList<IPipelineImplementationConfiguration> all = graphs;
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Success(all));
        return provider;
    }

    private static Mock<ICalculationConfigurationProvider> Calculations(string name, string? inputDataSet, string? outputDataSet)
    {
        var calculation = new Mock<ICalculationEntityImplementationConfiguration>();
        calculation.SetupGet(c => c.Name).Returns(name);
        calculation.SetupGet(c => c.OutputDataSetName).Returns(outputDataSet);
        var inputs = new List<CalculationInputConfigurationBase>();
        if (inputDataSet is not null)
            inputs.Add(new WindowedCalculationInputConfiguration { DataSetName = inputDataSet });
        calculation.SetupGet(c => c.Inputs).Returns(inputs);
        var provider = new Mock<ICalculationConfigurationProvider>();
        IReadOnlyList<ICalculationEntityImplementationConfiguration> all = [calculation.Object];
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ICalculationEntityImplementationConfiguration>>.Success(all));
        return provider;
    }

    private static Mock<IGlossaryTermConfigurationProvider> Terms(string name, string dataSet, string? field)
    {
        var term = new Mock<IGlossaryTermImplementationConfiguration>();
        term.SetupGet(t => t.Name).Returns(name);
        term.SetupGet(t => t.LinkedDataSets).Returns([new GlossaryTermLinkedDataSetConfiguration { DataSetName = dataSet, FieldName = field }]);
        var provider = new Mock<IGlossaryTermConfigurationProvider>();
        IReadOnlyList<IGlossaryTermImplementationConfiguration> all = [term.Object];
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IGlossaryTermImplementationConfiguration>>.Success(all));
        return provider;
    }

    private static Mock<IDataSetConfigurationProvider> DataSets(string name, string? derivedFrom)
    {
        var set = new Mock<IDataSetImplementationConfiguration>();
        set.SetupGet(s => s.Name).Returns(name);
        set.SetupGet(s => s.SourceDataSetName).Returns(derivedFrom);
        set.SetupGet(s => s.Sources).Returns([]);
        var provider = new Mock<IDataSetConfigurationProvider>();
        IReadOnlyList<IDataSetImplementationConfiguration> all = [set.Object];
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IDataSetImplementationConfiguration>>.Success(all));
        return provider;
    }

    private static Mock<ITransformationConfigurationProvider> Transformations(params (Guid Id, string Name)[] items)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        var all = items.Select(item =>
        {
            var transformation = new Mock<ITransformationImplementationConfiguration>();
            transformation.SetupGet(t => t.Id).Returns(item.Id);
            transformation.SetupGet(t => t.Name).Returns(item.Name);
            return transformation.Object;
        }).ToList();
        provider.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<ITransformationImplementationConfiguration>>.Success(all));
        return provider;
    }

    private static CatalogUses Uses(
        Mock<IPipelineConfigurationProvider>? pipelines = null,
        Mock<ICalculationConfigurationProvider>? calculations = null,
        Mock<IGlossaryTermConfigurationProvider>? terms = null,
        Mock<IDataSetConfigurationProvider>? dataSets = null,
        Mock<ITransformationConfigurationProvider>? transformations = null)
        => new(
            (dataSets ?? DataSets("Unrelated", null)).Object,
            (pipelines ?? Pipelines()).Object,
            (calculations ?? Calculations("Unrelated", null, null)).Object,
            (transformations ?? Transformations()).Object,
            (terms ?? Terms("Unrelated", "Elsewhere", null)).Object,
            NullLogger.Instance);

    private static string[] Described(CatalogUsesResponse response) =>
        [.. response.Items.Select(item => $"{item.Relation} {item.EntityType} {item.Name} [{string.Join("/", item.Parents)}]")];

    [Fact]
    public async Task WhatUsesADataSetIsThePipelinesThatReadAndWriteItTheCalculationsTheTermsAndTheDataSetsBuiltFromIt()
    {
        var uses = Uses(
            pipelines: Pipelines(Pipeline("Load", Source("Breweries"), Sink("Report")), Pipeline("Archive", Sink("Breweries"))),
            calculations: Calculations("Density", inputDataSet: "Breweries", outputDataSet: null),
            terms: Terms("Brewery", dataSet: "Breweries", field: null),
            dataSets: DataSets("BreweryRollup", derivedFrom: "Breweries"));

        var found = await uses.Run(CatalogKinds.DataSet, "breweries", [], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        Described(found.Value!).ShouldBe(
        [
            "derivedFrom DataSet BreweryRollup []",
            "reads Calculation Density []",
            "writes Pipeline Archive []",
            "reads Pipeline Load []",
            "describes GlossaryTerm Brewery []",
        ]);
        found.Value!.Total.ShouldBe(5);
    }

    [Fact]
    public async Task WhatAPipelineUsesIsTheDataSetsItReadsAndWrites()
    {
        var uses = Uses(pipelines: Pipelines(Pipeline("Load", Source("Breweries"), Sink("Report"))));

        var found = await uses.Run(CatalogKinds.Pipeline, "Load", [], CatalogUsesDirections.Uses, TestContext.Current.CancellationToken);

        Described(found.Value!).ShouldBe(["reads DataSet Breweries []", "writes DataSet Report []"]);
    }

    [Fact]
    public async Task ACalculationUsesItsInputAndOutputDataSets()
    {
        var uses = Uses(calculations: Calculations("Density", inputDataSet: "Breweries", outputDataSet: "BreweryStats"));

        var found = await uses.Run(CatalogKinds.Calculation, "Density", [], CatalogUsesDirections.Uses, TestContext.Current.CancellationToken);

        Described(found.Value!).ShouldBe(["reads DataSet Breweries []", "writes DataSet BreweryStats []"]);
    }

    [Fact]
    public async Task ATermLinkedToAFieldExplainsThatFieldUnderItsDataSet()
    {
        var uses = Uses(terms: Terms("Brewery type", dataSet: "Breweries", field: "BreweryType"));

        var found = await uses.Run(CatalogKinds.GlossaryTerm, "Brewery type", [], CatalogUsesDirections.Uses, TestContext.Current.CancellationToken);

        Described(found.Value!).ShouldBe(["describes DataSetField BreweryType [Breweries]"]);
    }

    [Fact]
    public async Task AContainerIsFoundOnlyUnderTheParentsTheQuestionStated()
    {
        var file = new PipelineComponentConfiguration { Name = "f", ComponentType = "ContainerSource", DataStoreName = "Landing", PathName = "inbound", ContainerName = "orders.csv" };
        var uses = Uses(pipelines: Pipelines(Pipeline("Ingest", file)));

        var right = await uses.Run(CatalogKinds.Container, "orders.csv", ["Landing", "inbound"], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);
        var wrong = await uses.Run(CatalogKinds.Container, "orders.csv", ["Landing", "outbound"], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);
        var any = await uses.Run(CatalogKinds.Container, "orders.csv", [], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);

        Described(right.Value!).ShouldBe(["reads Pipeline Ingest []"]);
        wrong.Value!.Items.ShouldBeEmpty();
        any.Value!.Total.ShouldBe(1);
    }

    [Fact]
    public async Task AThingNothingUsesAnswersAnEmptyListNotAnError()
    {
        var found = await Uses().Run(CatalogKinds.DataSet, "Orphan", [], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);

        found.IsSuccess.ShouldBeTrue();
        found.Value!.Items.ShouldBeEmpty();
        found.Value.Total.ShouldBe(0);
    }

    [Fact]
    public async Task OnlyTheProviderOfTheKindIsReadWhenAskingWhatAThingUses()
    {
        var calculations = Calculations("Density", "Breweries", null);
        var pipelines = Pipelines();

        await Uses(pipelines: pipelines, calculations: calculations).Run(CatalogKinds.Calculation, "Density", [], CatalogUsesDirections.Uses, TestContext.Current.CancellationToken);

        pipelines.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AReadThatFailsFailsTheQuestionRatherThanAnsweringWithoutIt()
    {
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines.Setup(p => p.Get(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IPipelineImplementationConfiguration>>.Failure(new GenericMessage("the store did not answer")));

        var found = await Uses(pipelines: pipelines).Run(CatalogKinds.DataSet, "Breweries", [], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);

        found.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void DirectionsAreReadWithoutRegardToCase()
    {
        CatalogUsesDirections.Of("USEDBY").ShouldBe("usedBy");
        CatalogUsesDirections.Of("sideways").ShouldBeNull();
        CatalogUsesDirections.Of(null).ShouldBeNull();
    }

    [Fact]
    public async Task AToolThatRunsANamedTransformationUsesItAndTheTransformationIsUsedByThePipeline()
    {
        var id = Guid.NewGuid();
        var compute = new PipelineComponentConfiguration
        {
            Name = "compute", ComponentType = "Compute", Stages = [new Fdw.Services.Etl.Transforms.PipelineTransformConfiguration { Name = "c", ComputeTransformationId = id }],
        };
        var uses = Uses(pipelines: Pipelines(Pipeline("Rollup", compute)), transformations: Transformations((id, "Margin")));

        var forward = await uses.Run(CatalogKinds.Pipeline, "Rollup", [], CatalogUsesDirections.Uses, TestContext.Current.CancellationToken);
        var backward = await uses.Run(CatalogKinds.Transformation, "Margin", [], CatalogUsesDirections.UsedBy, TestContext.Current.CancellationToken);

        Described(forward.Value!).ShouldBe(["uses Transformation Margin []"]);
        Described(backward.Value!).ShouldBe(["uses Pipeline Rollup []"]);
    }
}
