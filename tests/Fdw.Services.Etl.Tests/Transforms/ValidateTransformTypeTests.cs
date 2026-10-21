using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="ValidateTransformType"/> (FDW-1120): named checks asked of every record, Reject or Fail for a
/// failing record, and a stage that cannot say what it checks or what a failure causes refused rather than guessed.
/// </summary>
public sealed class ValidateTransformTypeTests
{
    private readonly ValidateTransformType _sut = new();

    private static TransformContext Context(params (string Name, object? Value)[] parameters)
    {
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in parameters)
            values[name] = value;
        return new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), parameters: values);
    }

    private static PipelineTransformConfiguration Stage(string? onFail, params (string Name, string Expression)[] checks) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Clean",
        OperationType = "Validate",
        OnFail = onFail,
        Checks = checks
            .Select((check, index) => new PipelineTransformCheckConfiguration { Name = check.Name, Expression = check.Expression, Ordinal = index + 1 })
            .ToList()
    };

    private static Dictionary<string, object?> Row(string label, int points, string? region = null) =>
        new() { ["Label"] = label, ["Points"] = points, ["Region"] = region };

    private static string[] Labels(IEnumerable<IDictionary<string, object?>> rows) => rows.Select(r => (string)r["Label"]!).ToArray();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task RejectDropsAFailingRecordAndReportsTheCheckAndTheStage()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 5), Row("b", -1), Row("c", 0) };
        var context = Context();

        // Act
        var result = await _sut.TransformBatch(inputs, Stage("Reject", ("Positive", "Points > 0")), context, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        Labels(result.Value!).ShouldBe(new[] { "a" });
        context.Errors.Count.ShouldBe(2);
        context.Errors[0].Message.ShouldContain("Positive");
        context.Errors[0].Message.ShouldContain("Clean");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ARecordIsReportedWithTheFirstCheckItFailsAndStopsThere()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", -1, null) };
        var context = Context();

        // Act
        await _sut.TransformBatch(inputs, Stage("Reject", ("Positive", "Points > 0"), ("HasRegion", "Region != null")), context, TestContext.Current.CancellationToken);

        // Assert: it fails both checks but is reported once, with the first
        context.Errors.Count.ShouldBe(1);
        context.Errors[0].Message.ShouldContain("Positive");
        context.Errors[0].Message.ShouldNotContain("HasRegion");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task FailStopsTheRunAtTheFirstFailingRecordNamingTheCheck()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 5), Row("b", -1) };

        // Act
        var result = await _sut.TransformBatch(inputs, Stage("Fail", ("Positive", "Points > 0")), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(21001);
        result.Details!.GetValue<string>("CheckName").ShouldBe("Positive");
        result.Details!.GetValue<string>("StageName").ShouldBe("Clean");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task EveryRecordThatPassesEveryCheckIsKeptInOrder()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 1, "EU"), Row("b", 2, "US"), Row("c", 3, "EU") };

        // Act
        var result = await _sut.TransformBatch(inputs, Stage("Reject", ("Positive", "Points > 0"), ("InEurope", "Region == EU")), Context(), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "a", "c" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task ACheckCanReferenceARunParameter()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("low", 1), Row("high", 9) };

        // Act
        var result = await _sut.TransformBatch(inputs, Stage("Reject", ("AtLeast", "Points >= {{MinPoints}}")), Context(("MinPoints", 5L)), TestContext.Current.CancellationToken);

        // Assert
        Labels(result.Value!).ShouldBe(new[] { "high" });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task ACheckThatCannotBeReadFailsTheStageBeforeAnyRecordAndNeverPasses()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 5) };

        // Act
        var result = await _sut.TransformBatch(inputs, Stage("Reject", ("Broken", "garbage expression")), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(21000);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ignore")]
    public async Task AStageThatDoesNotSayWhatAFailureCausesFailsNoDefault(string? onFail)
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { Row("a", 5) };

        // Act
        var result = await _sut.TransformBatch(inputs, Stage(onFail, ("Positive", "Points > 0")), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public async Task AStageWithNoChecksFails()
    {
        // Act
        var result = await _sut.TransformBatch([Row("a", 5)], Stage("Reject"), Context(), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("TRANSFORM-20101");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public async Task TransformJudgesASingleRecordTheSameWay()
    {
        // Arrange
        var keep = Row("a", 5);
        var drop = Row("b", -1);
        var stage = Stage("Reject", ("Positive", "Points > 0"));

        // Act
        var kept = await _sut.Transform(keep, stage, Context(), TestContext.Current.CancellationToken);
        var dropped = await _sut.Transform(drop, stage, Context(), TestContext.Current.CancellationToken);

        // Assert
        kept.Value.ShouldBeSameAs(keep);
        dropped.IsSuccess.ShouldBeTrue();
        dropped.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheKindOwnsChecksAndOnFailAndFiltersRecords()
    {
        _sut.Members.Select(member => member.Name).ShouldBe(new[] { "checks", "onFail" });
        _sut.Members.ShouldAllBe(member => member.IsRequired);
        _sut.CanFilterRecords.ShouldBeTrue();
        _sut.RequiresCompleteInput.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void EveryStageKindListsTheMembersItOwns()
    {
        // Act: this class constructs a stage kind, so the Etl assembly is loaded and its kinds are registered
        var owned = Fdw.Services.Etl.Abstractions.OptionTypes.TransformTypes.All()
            .ToDictionary(kind => kind.Name, kind => string.Join(",", ((IStageMemberShape)kind).Members.Select(member => member.Name)));

        // Assert
        owned["Filter"].ShouldBe("filterExpression");
        owned["Validate"].ShouldBe("checks,onFail");
        owned["Sort"].ShouldBe("sort");
        owned["LatestBy"].ShouldBe("latestBy");
        owned["Distinct"].ShouldBe("distinct");
        owned["Aggregate"].ShouldBe("aggregation");
        owned["Calculate"].ShouldBe("calculation");
        owned["Lookup"].ShouldBe("lookup");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void TheFailingActionsAreRejectAndFail()
    {
        ValidateFailActions.ByName("Reject").StopsRun.ShouldBeFalse();
        ValidateFailActions.ByName("Fail").StopsRun.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationCopiesTheChecksInOrderCountedFromOne()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Clean",
            OperationType = "Validate",
            OnFail = "Reject",
            Checks = [new FakeCheckSpec("Positive", "Points > 0"), new FakeCheckSpec("InEurope", "Region == EU")]
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Clean", OperationType = "Validate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        target.OnFail.ShouldBe("Reject");
        target.Checks.Select(check => (check.Name, check.Expression, check.Ordinal))
            .ShouldBe(new[] { ("Positive", "Points > 0", 1), ("InEurope", "Region == EU", 2) });
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null, "Positive", "Points > 0")]
    [InlineData("Ignore", "Positive", "Points > 0")]
    [InlineData("Reject", "", "Points > 0")]
    [InlineData("Reject", "Positive", "")]
    public void MapSpecToConfigurationRefusesAStageThatIsIncomplete(string? onFail, string checkName, string expression)
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Clean",
            OperationType = "Validate",
            OnFail = onFail,
            Checks = [new FakeCheckSpec(checkName, expression)]
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Clean", OperationType = "Validate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        target.Checks.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesNoChecksAndRepeatedCheckNames()
    {
        // Arrange
        var none = new FakeTransformOperationSpec { Name = "Clean", OperationType = "Validate", OnFail = "Reject" };
        var twice = new FakeTransformOperationSpec
        {
            Name = "Clean",
            OperationType = "Validate",
            OnFail = "Reject",
            Checks = [new FakeCheckSpec("Positive", "Points > 0"), new FakeCheckSpec("positive", "Points < 99")]
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Clean", OperationType = "Validate" };

        // Act and assert
        _sut.MapSpecToConfiguration(none, target, NullLogger.Instance).IsSuccess.ShouldBeFalse();
        _sut.MapSpecToConfiguration(twice, target, NullLogger.Instance).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesACheckThatCannotBeRead()
    {
        // Arrange
        var spec = new FakeTransformOperationSpec
        {
            Name = "Clean",
            OperationType = "Validate",
            OnFail = "Reject",
            Checks = [new FakeCheckSpec("Broken", "Points >")]
        };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Clean", OperationType = "Validate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert: refused when saved, with the text named
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(21000);
        result.Details!.GetValue<string>("Expression").ShouldBe("Points >");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationRefusesASpecThatStatesNoChecks()
    {
        // Arrange: a spec type that has no Validate part cannot describe a Validate stage
        var spec = new PlainSpec { Name = "Clean", OperationType = "Validate" };
        var target = new PipelineTransformConfiguration { Id = Guid.NewGuid(), Name = "Clean", OperationType = "Validate" };

        // Act
        var result = _sut.MapSpecToConfiguration(spec, target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    private sealed class PlainSpec : ITransformOperationSpec
    {
        public string Name { get; set; } = string.Empty;

        public string OperationType { get; set; } = string.Empty;

        public int ExecutionOrder { get; set; }

        public bool IsEnabled { get; set; } = true;

        public IReadOnlyList<string> GroupByFields { get; set; } = [];

        public IReadOnlyList<IAggregationSpec> Aggregations { get; set; } = [];

        public ILookupSpec? Lookup { get; set; }

        public IReadOnlyList<ICalculationSpec> ComputedColumns { get; set; } = [];

        public string? FilterExpression { get; set; }
    }
}
