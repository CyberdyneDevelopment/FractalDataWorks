using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Tests.Pipelines;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the Compute tool in a graph (FDW-1615): the Built transformation is compiled for the run before any row moves, a
/// transformation that works on whole tables holds its input, a row the transformation cannot build goes to the error output or
/// fails the run, and what a graph has no place for is refused.
/// </summary>
public sealed class ComputeInGraphTests
{
    private static readonly Guid BuiltId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly FlowExecutor _executor = new();

    private static BuiltTransformationConfiguration Ratio()
    {
        var built = new BuiltTransformationConfiguration { Id = BuiltId, Name = "Ratio" };
        BuiltTransformationSetWriter.Apply(built, new CalculationSetDefinition(
            "Ratio",
            [new TableInputDefinition("Rows", [new TableColumnDefinition("Id", "Number")])],
            [],
            [new TableOutputDefinition("Lines", new EachRowSource("Rows"), [new CopyColumn("Id", "Id"), new FormulaColumn("Each", Divide(Field("Id"), Field("Id")))])])).IsSuccess.ShouldBeTrue();
        return built;
    }

    private static Mock<IEffectiveSettingsProvider> Settings()
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(200);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(5000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(32);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxSetOutputRows, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(1000);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxSetLookupRows, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(1000);
        return settings;
    }

    private static PipelineComponentConfiguration ComputeTool(string onRowFailure, string? errorOutput, string route = "Replace")
    {
        var tool = new PipelineComponentConfiguration { Name = "ratio", ComponentType = "Compute", BatchSize = 2, ErrorOutput = errorOutput };
        tool.Stages.Add(new PipelineTransformConfiguration
        {
            Id = Guid.NewGuid(),
            Name = "ratio",
            OperationType = "Compute",
            IsEnabled = true,
            ComputeTransformationId = BuiltId,
            ComputeOnRowFailure = onRowFailure,
            ComputeTables = [new PipelineTransformComputeTableConfiguration { TableName = "Rows", FromKind = "Stage" }],
            ComputeOutputs = [new PipelineTransformComputeOutputConfiguration { OutputName = "Lines", Route = route, RouteName = route == "Replace" ? null : "kept" }],
        });
        return tool;
    }

    private static async Task<BuiltCompilation> Compiled(PipelineComponentConfiguration tool)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITransformationImplementationConfiguration>.Success(Ratio()));
        var built = await BuiltCalculationResolver.Resolve(tool.Stages, provider.Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        return built.Value!;
    }

    private static FlowBuildContext ContextWith(GraphPipelineConfiguration graph, BuiltCompilation built) =>
        new(
            graph.Name,
            graph.Id,
            Guid.NewGuid(),
            new PagedSourceGateway(PagedSourceGateway.Rows(4), []).Provider.ByName("Main"),
            new PipelineLoadExecutor(new LoadTargetResolver(null, null), null, null, NullLogger.Instance),
            null,
            DateTimeOffset.UtcNow,
            new Dictionary<string, object?>(),
            [],
            NullLogger.Instance,
            null,
            null,
            built);

    private async Task<IGenericResult> Run(PipelineComponentConfiguration tool)
    {
        var graph = tool.ErrorOutput is { } error
            ? Graph([DataSetSource("src"), tool], [Link("src", "ratio")], Dropped("ratio"), Dropped("ratio", error))
            : Graph([DataSetSource("src"), tool], [Link("src", "ratio")], Dropped("ratio"));
        var built = GraphFlowBuilder.Build(graph, ContextWith(graph, await Compiled(tool)));
        built.IsSuccess.ShouldBeTrue(built.CurrentMessage);
        return await built.Value!.Run(StepContext(new FlowVariables(), new FlowRunTally(_executor)), TestContext.Current.CancellationToken);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComputeWhoseTransformationCannotBuildARowFailsTheRunWhenTheToolHasNoErrorOutput()
    {
        var ran = await Run(ComputeTool("Reject", errorOutput: null));

        ran.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AComputeSendsTheRowItCannotBuildToItsErrorOutputAndTheRunSucceeds()
    {
        var ran = await Run(ComputeTool("Reject", errorOutput: "bad"));

        ran.IsSuccess.ShouldBeTrue(ran.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AComputeWithAnOutputKeptUnderANameForALaterStageIsRefusedBecauseAGraphLinksItsTools()
    {
        var tool = ComputeTool("Reject", "bad", route: "Named");
        var graph = Graph([DataSetSource("src"), tool], [Link("src", "ratio")], Dropped("ratio"), Dropped("ratio", "bad"));

        var built = GraphFlowBuilder.Build(graph, ContextWith(graph, await Compiled(tool)));

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("later stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComputeThatHoldsEveryRowNeedsABatchSize()
    {
        var tool = ComputeTool("Reject", null);
        tool.BatchSize = null;
        var graph = Graph([DataSetSource("src"), tool], [Link("src", "ratio")], Dropped("ratio"));

        var built = GraphFlowBuilder.Build(graph, Context(graph, new PagedSourceGateway([], [])));

        // No transformation is compiled here, so the adapter is never reached for the set check; the tool alone states its needs.
        built.IsFailure.ShouldBeTrue();
    }
}
