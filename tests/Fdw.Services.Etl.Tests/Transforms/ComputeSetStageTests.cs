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
/// FDW-1231: a Compute stage whose Built transformation reads and writes tables. The run resolves and checks every binding when
/// it starts; the stage fills each table from the place it names, runs the set, and sends each output where it is routed: the
/// Replace output becomes the stage's rows, a Named output is kept for later stages.
/// </summary>
public sealed class ComputeSetStageTests
{
    private static readonly Guid BuiltId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static BuiltTransformationConfiguration FeeSet()
    {
        var built = new BuiltTransformationConfiguration { Id = BuiltId, Name = "FeeSet" };
        built.Inputs.Add(new BuiltTransformationInputConfiguration { Name = "pct", ValueType = "Number", ExecutionOrder = 0 });
        BuiltTransformationSetWriter.Apply(built, new CalculationSetDefinition(
            "FeeSet",
            [new TableInputDefinition("Trades", [new TableColumnDefinition("TradeId", "Text"), new TableColumnDefinition("Notional", "Number")])],
            [],
            [
                new TableOutputDefinition(
                    "Fees",
                    new EachRowSource("Trades"),
                    [new CopyColumn("TradeId", "TradeId"), new FormulaColumn("Amount", Round(Multiply(Field("Notional"), Field("pct")), 2, "AwayFromZero"))]),
                new TableOutputDefinition(
                    "Kept",
                    new EachRowSource("Trades", GreaterOrEqual(Field("Notional"), Constant(1000m))),
                    [new CopyColumn("TradeId", "TradeId"), new CopyColumn("Notional", "Notional")]),
            ])).IsSuccess.ShouldBeTrue();
        return built;
    }

    private static Mock<ITransformationConfigurationProvider> Provider(BuiltTransformationConfiguration built)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == built.Id
                ? GenericResult<ITransformationImplementationConfiguration>.Success(built)
                : GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found")));
        return provider;
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

    private static PipelineTransformConfiguration Stage() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Settle",
        OperationType = "Compute",
        ComputeTransformationId = BuiltId,
        ComputeOnRowFailure = "Fail",
        ComputeTables = [new PipelineTransformComputeTableConfiguration { TableName = "Trades", FromKind = "Stage" }],
        ComputeOutputs =
        [
            new PipelineTransformComputeOutputConfiguration { OutputName = "Fees", Route = "Named", RouteName = "FeeLines" },
            new PipelineTransformComputeOutputConfiguration { OutputName = "Kept", Route = "Replace" },
        ],
        ComputeScalars = [new PipelineTransformComputeScalarConfiguration { InputName = "pct", ValueText = "0.01", ValueType = "Number" }],
    };

    private static List<IDictionary<string, object?>> Trades() =>
    [
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["TradeId"] = "T1", ["Notional"] = 999.00m },
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["TradeId"] = "T2", ["Notional"] = 1025.00m },
    ];

    private static async Task<BuiltCompilation> Resolve(PipelineTransformConfiguration stage)
    {
        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(FeeSet()).Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        resolved.IsSuccess.ShouldBeTrue(resolved.CurrentMessage);
        return resolved.Value!;
    }

    private static TransformContext Context(BuiltCompilation compiled) => new(
        Guid.NewGuid(),
        NullLogger.Instance,
        new Dictionary<string, object?>(),
        new Dictionary<Guid, IReadOnlyList<IFieldMappingTransform>>(),
        builtCalculations: compiled.Calculations,
        builtSets: compiled.Sets);

    [Fact]
    public async Task TheReplaceOutputBecomesTheStagesRowsAndANamedOutputIsKeptForLaterStages()
    {
        var stage = Stage();
        var compiled = await Resolve(stage);
        var context = Context(compiled);

        var result = await new ComputeTransformType().TransformBatch(Trades(), stage, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var rows = result.Value!.ToList();
        rows.Count.ShouldBe(1);
        rows[0]["TradeId"].ShouldBe("T2");
        var kept = context.SharedState["compute.output.FeeLines"].ShouldBeOfType<List<IReadOnlyDictionary<string, object?>>>();
        kept.Select(row => row["Amount"]).ShouldBe(new object[] { 9.99m, 10.25m });
    }

    [Fact]
    public async Task WithNoReplaceRouteTheRowsThatArrivedCarryOnUnchanged()
    {
        var stage = Stage();
        stage.ComputeOutputs = [.. stage.ComputeOutputs.Where(route => route.OutputName == "Fees"), new PipelineTransformComputeOutputConfiguration { OutputName = "Kept", Route = "Named", RouteName = "Big" }];
        var compiled = await Resolve(stage);

        var result = await new ComputeTransformType().TransformBatch(Trades(), stage, Context(compiled), TestContext.Current.CancellationToken);

        result.Value!.Count().ShouldBe(2);
    }

    [Fact]
    public async Task ARunThatHandlesOneRecordAtATimeRefusesATablesStage()
    {
        var stage = Stage();
        var compiled = await Resolve(stage);

        var result = await new ComputeTransformType().Transform(Trades()[0], stage, Context(compiled), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ATableTheStageDoesNotBindRefusesTheRunBeforeAnyRecordMoves()
    {
        var stage = Stage();
        stage.ComputeTables = [];

        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(FeeSet()).Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
        resolved.CurrentMessage.ShouldNotBeNull().ShouldContain("Trades");
    }

    [Fact]
    public async Task AnOutputRoutedNowhereTwoReplaceRoutesAndAMissingValueAreEachRefused()
    {
        var unrouted = Stage();
        unrouted.ComputeOutputs = [unrouted.ComputeOutputs[0]];
        var twoReplace = Stage();
        twoReplace.ComputeOutputs = [new PipelineTransformComputeOutputConfiguration { OutputName = "Fees", Route = "Replace" }, twoReplace.ComputeOutputs[1]];
        var noValue = Stage();
        noValue.ComputeScalars = [];

        foreach (var stage in new[] { unrouted, twoReplace, noValue })
        {
            var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(FeeSet()).Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);
            resolved.IsFailure.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task AFieldInputOnATablesStageIsRefused()
    {
        var stage = Stage();
        stage.ComputeInputs = [new PipelineTransformComputeInputConfiguration { InputName = "pct", SourceField = "x" }];

        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(FeeSet()).Object, Settings().Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
    }
}
