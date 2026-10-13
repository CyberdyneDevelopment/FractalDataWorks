using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Sets;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// FDW-1246: what a Compute stage that reads and writes tables does with a row it cannot build is the author's decision. Fail
/// stops the stage, Skip drops the row and reports it, Reject drops it and keeps it with the stage's rejects; a stage that
/// states none, or one that does not exist, is refused when the run starts.
/// </summary>
public sealed class ComputeRowFailureTests
{
    private static readonly Guid BuiltId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private static BuiltTransformationConfiguration PerUnit()
    {
        var built = new BuiltTransformationConfiguration { Id = BuiltId, Name = "PerUnit" };
        BuiltTransformationSetWriter.Apply(built, new CalculationSetDefinition(
            "PerUnit",
            [new TableInputDefinition("Trades", [new TableColumnDefinition("TradeId", "Text"), new TableColumnDefinition("Notional", "Number"), new TableColumnDefinition("Qty", "Number")])],
            [],
            [new TableOutputDefinition("Lines", new EachRowSource("Trades"), [new CopyColumn("TradeId", "TradeId"), new FormulaColumn("Each", Divide(Field("Notional"), Field("Qty")))])])).IsSuccess.ShouldBeTrue();
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

    private static PipelineTransformConfiguration Stage(string? onRowFailure) => new()
    {
        Id = Guid.NewGuid(),
        Name = "PerUnit",
        OperationType = "Compute",
        ComputeTransformationId = BuiltId,
        ComputeOnRowFailure = onRowFailure,
        ComputeTables = [new PipelineTransformComputeTableConfiguration { TableName = "Trades", FromKind = "Stage" }],
        ComputeOutputs = [new PipelineTransformComputeOutputConfiguration { OutputName = "Lines", Route = "Replace" }],
    };

    private static async Task<IGenericResult<BuiltCompilation>> Resolve(PipelineTransformConfiguration stage)
    {
        var built = PerUnit();
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<ITransformationImplementationConfiguration>.Success(built));
        return await BuiltCalculationResolver.Resolve([stage], provider.Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);
    }

    private static List<IDictionary<string, object?>> Trades() =>
    [
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["TradeId"] = "T1", ["Notional"] = 10m, ["Qty"] = 2m },
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["TradeId"] = "T2", ["Notional"] = 10m, ["Qty"] = 0m },
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["TradeId"] = "T3", ["Notional"] = 10m, ["Qty"] = 5m },
    ];

    private static async Task<(IGenericResult<IEnumerable<IDictionary<string, object?>>> Result, TransformContext Context)> Run(string action)
    {
        var stage = Stage(action);
        var resolved = await Resolve(stage);
        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        var context = new TransformContext(
            Guid.NewGuid(),
            NullLogger.Instance,
            new Dictionary<string, object?>(),
            new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>(),
            builtCalculations: resolved.Value!.Calculations,
            builtSets: resolved.Value.Sets);
        var result = await new ComputeTransformType().TransformBatch(Trades(), stage, context, TestContext.Current.CancellationToken);
        return (result, context);
    }

    [Fact]
    public async Task FailStopsTheStageAndWritesNothing()
    {
        var (result, context) = await Run("Fail");

        result.IsFailure.ShouldBeTrue();
        context.Rejections.ShouldBeEmpty();
    }

    [Fact]
    public async Task SkipDropsTheRowAndReportsItAsAnError()
    {
        var (result, context) = await Run("Skip");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => row["TradeId"]).ShouldBe(["T1", "T3"]);
        context.Errors.Count.ShouldBe(1);
        context.Rejections.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejectDropsTheRowAndKeepsItWithTheStagesRejects()
    {
        var (result, context) = await Run("Reject");

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        result.Value!.Select(row => row["TradeId"]).ShouldBe(["T1", "T3"]);
        context.Rejections.Count.ShouldBe(1);
        context.Rejections[0].Stage.ShouldBe("PerUnit");
        context.Rejections[0].Record["TradeId"].ShouldBe("T2");
    }

    [Fact]
    public async Task AStageThatStatesNoActionOrAnUnknownOneIsRefusedWhenTheRunStarts()
    {
        var none = await Resolve(Stage(null));
        var blank = await Resolve(Stage(" "));
        var unknown = await Resolve(Stage("Ignore"));

        none.IsFailure.ShouldBeTrue();
        none.CurrentMessage.ShouldNotBeNull().ShouldContain("onRowFailure");
        blank.IsFailure.ShouldBeTrue();
        unknown.IsFailure.ShouldBeTrue();
        unknown.CurrentMessage.ShouldNotBeNull().ShouldContain("Ignore");
    }

    [Fact]
    public void OnlyFailSkipAndRejectAreKnownAndTheyAreSpelledExactly()
    {
        RowFailureActions.All.ShouldBe(["Fail", "Skip", "Reject"]);
        RowFailureActions.IsKnown("Skip").ShouldBeTrue();
        RowFailureActions.IsKnown("skip").ShouldBeFalse();
        RowFailureActions.IsKnown(null).ShouldBeFalse();
    }
}
