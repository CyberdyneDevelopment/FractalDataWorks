using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Transformations;
using Fdw.Data.Transformations.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions.Fluency;
using Fdw.Services.Calculations.Abstractions.Trees;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Settings;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Calculations.Abstractions.Fluency.Calc;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for the Compute stage and <see cref="BuiltCalculationResolver"/> (FDW-1209): a Built transformation is resolved and
/// compiled once when the run starts, anything that cannot be resolved fails the run naming the stage, and the stage adds the
/// kept results to each record or reports the calculation's own coded failure.
/// </summary>
public sealed class ComputeTransformTypeTests
{
    private static readonly Guid BuiltId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly CalculationLimits Limits = new(200, 5000, 32);

    private static readonly IReadOnlyList<BuiltTransformationInputConfiguration> Declared = new List<BuiltTransformationInputConfiguration>
    {
        new() { Name = "intervalMinutes", ValueType = "Number", ExecutionOrder = 0 },
        new() { Name = "quantity", ValueType = "Number", ExecutionOrder = 1 },
        new() { Name = "price", ValueType = "Number", ExecutionOrder = 2 },
        new() { Name = "share", ValueType = "Number", ExecutionOrder = 3 },
        new() { Name = "fee", ValueType = "Number", ExecutionOrder = 4 },
    };

    private static BuiltTransformationConfiguration Settlement()
    {
        var array = CalculationArray.Named("Settlement")
            .Result("hours", Divide(60, Field("intervalMinutes")), emit: false)
            .Result("energy", Divide(Field("quantity"), Ref("hours")), emit: false)
            .Result("amount", When(NotEqual(Field("share"), 0), Multiply(Ref("energy"), Field("price"), Field("share")), 0))
            .Result("total", Add(Ref("amount"), Field("fee")))
            .Build(Limits)
            .Value!;
        var built = BuiltTransformationConverter.ToConfiguration("Settlement", array, Declared);
        built.Id = BuiltId;
        return built;
    }

    private static Mock<ITransformationConfigurationProvider> Provider(params ITransformationImplementationConfiguration[] known)
    {
        var provider = new Mock<ITransformationConfigurationProvider>();
        provider
            .Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                known.FirstOrDefault(item => item.Id == id) is { } found
                    ? GenericResult<ITransformationImplementationConfiguration>.Success(found)
                    : GenericResult<ITransformationImplementationConfiguration>.Failure(new GenericMessage("not found")));
        return provider;
    }

    private static Mock<IEffectiveSettingsProvider> Settings(int results, int nodes, int depth)
    {
        var settings = new Mock<IEffectiveSettingsProvider>();
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationResults, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(results);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationNodes, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(nodes);
        settings.Setup(s => s.GetEffectiveValue<int>(SettingDefinitions.MaxCalculationDepth, It.IsAny<Guid?>(), It.IsAny<string?>())).Returns(depth);
        return settings;
    }

    private static PipelineTransformConfiguration ComputeStage(params string[] unmapped)
    {
        var mapped = Declared.Select(input => input.Name).Where(name => !unmapped.Contains(name)).ToList();
        return new PipelineTransformConfiguration
        { IsEnabled = true,
            Id = Guid.NewGuid(),
            Name = "Settle",
            OperationType = "Compute",
            ComputeTransformationId = BuiltId,
            ComputeInputs = [.. mapped.Select(name => new PipelineTransformComputeInputConfiguration { InputName = name, SourceField = "src_" + name })],
        };
    }

    private static Dictionary<string, object?> Interval(decimal minutes) => new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = 7,
        ["src_intervalMinutes"] = minutes,
        ["src_quantity"] = 42.5m,
        ["src_price"] = 31.20m,
        ["src_share"] = 0.40m,
        ["src_fee"] = 0m,
    };

    private static async Task<(TransformContext Context, PipelineTransformConfiguration Stage)> ResolvedRun()
    {
        var stage = ComputeStage();
        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(Settlement()).Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        resolved.IsSuccess.ShouldBeTrue();
        var context = new TransformContext(
            Guid.NewGuid(),
            NullLogger.Instance,
            new Dictionary<string, object?>(),
            builtCalculations: resolved.Value!.Calculations);
        return (context, stage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheStageAddsTheKeptResultsAndKeepsTheRecord()
    {
        var (context, stage) = await ResolvedRun();

        var result = await new ComputeTransformType().Transform(Interval(15m), stage, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!["amount"].ShouldBe(132.60m);
        result.Value["total"].ShouldBe(132.60m);
        result.Value.ContainsKey("hours").ShouldBeFalse();
        result.Value.ContainsKey("energy").ShouldBeFalse();
        result.Value["id"].ShouldBe(7);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADateResultIsWrittenAsADateNotAsANumber()
    {
        var array = CalculationArray.Named("Due")
            .Result("due", AddDate(Field("start"), 30, "Days"))
            .Build(Limits, allowDateTimeResults: true)
            .Value!;
        var built = BuiltTransformationConverter.ToConfiguration(
            "Due", array, [new BuiltTransformationInputConfiguration { Name = "start", ValueType = "Date", ExecutionOrder = 0 }]);
        built.Id = BuiltId;
        var stage = new PipelineTransformConfiguration
        { IsEnabled = true,
            Id = Guid.NewGuid(),
            Name = "Due",
            OperationType = "Compute",
            ComputeTransformationId = BuiltId,
            ComputeInputs = [new PipelineTransformComputeInputConfiguration { InputName = "start", SourceField = "src_start" }],
        };
        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(built).Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        resolved.IsSuccess.ShouldBeTrue();
        var context = new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), builtCalculations: resolved.Value!.Calculations);

        var dateOnly = await new ComputeTransformType().Transform(
            new Dictionary<string, object?> { ["id"] = 7, ["src_start"] = new DateOnly(2026, 10, 22) }, stage, context, TestContext.Current.CancellationToken);
        var dateColumn = await new ComputeTransformType().Transform(
            new Dictionary<string, object?> { ["id"] = 8, ["src_start"] = new DateTime(2026, 10, 22) }, stage, context, TestContext.Current.CancellationToken);

        dateOnly.IsSuccess.ShouldBeTrue();
        dateOnly.Value!["due"].ShouldBe(new DateOnly(2026, 11, 21));
        dateOnly.Value["id"].ShouldBe(7);
        dateColumn.IsSuccess.ShouldBeTrue();
        dateColumn.Value!["due"].ShouldBe(new DateOnly(2026, 11, 21));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADateFieldWithATimeOfDayFailsTheRecordInsteadOfTruncating()
    {
        var array = CalculationArray.Named("Due").Result("due", AddDate(Field("start"), 1, "Days")).Build(Limits, allowDateTimeResults: true).Value!;
        var built = BuiltTransformationConverter.ToConfiguration(
            "Due", array, [new BuiltTransformationInputConfiguration { Name = "start", ValueType = "Date", ExecutionOrder = 0 }]);
        built.Id = BuiltId;
        var stage = new PipelineTransformConfiguration
        { IsEnabled = true,
            Id = Guid.NewGuid(),
            Name = "Due",
            OperationType = "Compute",
            ComputeTransformationId = BuiltId,
            ComputeInputs = [new PipelineTransformComputeInputConfiguration { InputName = "start", SourceField = "src_start" }],
        };
        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(built).Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        var context = new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), builtCalculations: resolved.Value!.Calculations);

        var result = await new ComputeTransformType().Transform(
            new Dictionary<string, object?> { ["src_start"] = new DateTime(2026, 10, 22, 9, 30, 0) }, stage, context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code?.Name.ShouldBe("FieldNotDate");
    }

    private static async Task<(TransformContext Context, PipelineTransformConfiguration Stage)> NowRun(TimeProvider? clock)
    {
        var array = CalculationArray.Named("Stamp").Result("stamp", Now("UTC")).Build(Limits, allowDateTimeResults: true).Value!;
        var built = BuiltTransformationConverter.ToConfiguration("Stamp", array, []);
        built.Id = BuiltId;
        var stage = new PipelineTransformConfiguration
        { IsEnabled = true,
            Id = Guid.NewGuid(),
            Name = "Stamp",
            OperationType = "Compute",
            ComputeTransformationId = BuiltId,
        };
        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider(built).Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);
        resolved.IsSuccess.ShouldBeTrue();
        var context = new TransformContext(
            Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), builtCalculations: resolved.Value!.Calculations, clock: clock);
        return (context, stage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANowCalculationReadsTheRunsClockAndEveryRecordSeesTheSameMoment()
    {
        var (context, stage) = await NowRun(new SteppingClock(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero)));

        var first = await new ComputeTransformType().Transform(new Dictionary<string, object?> { ["id"] = 1 }, stage, context, TestContext.Current.CancellationToken);
        var second = await new ComputeTransformType().Transform(new Dictionary<string, object?> { ["id"] = 2 }, stage, context, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeTrue();
        first.Value!["stamp"].ShouldBe(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
        second.IsSuccess.ShouldBeTrue();
        second.Value!["stamp"].ShouldBe(first.Value["stamp"]);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ANowCalculationInARunWithNoClockFailsTheRecordWithClockMissing()
    {
        var (context, stage) = await NowRun(null);

        var result = await new ComputeTransformType().Transform(new Dictionary<string, object?> { ["id"] = 1 }, stage, context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code?.Name.ShouldBe("ClockMissing");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ADivisionByZeroFailsTheRecordWithItsCode()
    {
        var (context, stage) = await ResolvedRun();

        var result = await new ComputeTransformType().Transform(Interval(0m), stage, context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Code?.Name.ShouldBe("DivisionByZero");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARecordThatLacksAMappedFieldFailsInsteadOfReadingAnEmptyValue()
    {
        var (context, stage) = await ResolvedRun();
        var record = Interval(15m);
        record.Remove("src_price");

        var result = await new ComputeTransformType().Transform(record, stage, context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AStageTheRunDidNotResolveFailsInsteadOfComputingNothing()
    {
        var stage = ComputeStage();
        var context = new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>());

        var result = await new ComputeTransformType().Transform(Interval(15m), stage, context, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task APipelineWithNoComputeStageNeedsNoProviderAndNoSettings()
    {
        var stage = new PipelineTransformConfiguration { IsEnabled = true, Id = Guid.NewGuid(), Name = "Shape", OperationType = "Filter" };

        var resolved = await BuiltCalculationResolver.Resolve([stage], null, null, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsSuccess.ShouldBeTrue();
        resolved.Value!.Calculations.Count.ShouldBe(0);
        resolved.Value.Sets.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATransformationThatNoLongerResolvesFailsTheRun()
    {
        var stage = ComputeStage();

        var resolved = await BuiltCalculationResolver.Resolve([stage], Provider().Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ATransformationThatIsNotBuiltFailsTheRun()
    {
        var notBuilt = new FieldTransformationConfiguration { Id = BuiltId, Name = "Round", Implementation = FieldTransformationConfiguration.ImplementationName };

        var resolved = await BuiltCalculationResolver.Resolve([ComputeStage()], Provider(notBuilt).Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AnInputWithNoMappedFieldFailsTheRun()
    {
        var resolved = await BuiltCalculationResolver.Resolve([ComputeStage("fee")], Provider(Settlement()).Object, Settings(200, 5000, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task AMissingLimitSettingFailsTheRun()
    {
        var resolved = await BuiltCalculationResolver.Resolve([ComputeStage()], Provider(Settlement()).Object, Settings(200, 0, 32).Object, NullLogger.Instance, TestContext.Current.CancellationToken);

        resolved.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ABatchSkipsTheFailingRecordAndKeepsTheRest()
    {
        var (context, stage) = await ResolvedRun();

        var result = await new ComputeTransformType().TransformBatch([Interval(15m), Interval(0m), Interval(60m)], stage, context, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count().ShouldBe(2);
    }

    // A clock that moves on every read, to show the stage reads it once for the run and not once per record.
    private sealed class SteppingClock : TimeProvider
    {
        private DateTimeOffset _now;

        public SteppingClock(DateTimeOffset start) => _now = start;

        public override DateTimeOffset GetUtcNow()
        {
            var read = _now;
            _now = _now.AddSeconds(5);
            return read;
        }
    }
}
