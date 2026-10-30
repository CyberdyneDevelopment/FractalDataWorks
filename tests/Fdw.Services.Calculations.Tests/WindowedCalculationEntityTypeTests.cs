using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Configuration;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="WindowedCalculationEntityType"/>: typed-configuration validation, row
/// aggregation across inputs, dispatch of the configured window function through
/// <c>WindowedCalculationTypes</c>, and the sealed <see cref="CalculationEntityBase{TConfiguration}"/>
/// dispatch. Partition and order fields are not part of the configuration yet and are not covered.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "CoreFramework")]
public class WindowedCalculationEntityTypeTests
{
    private static ResolvedCalculationInput DataSetInput(string alias, params Dictionary<string, object>[] rows)
        => new()
        {
            InputAlias = alias,
            Kind = CalculationInputKinds.ByName("DataSet"),
            ResolvedValue = new List<Dictionary<string, object>>(rows)
        };

    private static TestCalculationEntity EntityFor(string windowFunction, string targetField = "Score")
        => new()
        {
            CalculationEntityType = "Windowed",
            TypedConfiguration = new WindowedCalculationConfiguration
            {
                TargetField = targetField,
                WindowFunction = windowFunction,
                OutputFieldName = "Result"
            }
        };

    private static ResolvedCalculationInput ScoreInput(params decimal[] scores)
        => DataSetInput("A", [.. scores.Select(score => new Dictionary<string, object> { ["Score"] = score })]);

    private static async Task<List<decimal?>> RunColumn(string windowFunction, params decimal[] scores)
    {
        var result = await new WindowedCalculationEntityType().Execute(
            EntityFor(windowFunction), [ScoreInput(scores)], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        return [.. doc.RootElement.GetProperty("Rows").EnumerateArray().Select(row =>
            row.GetProperty("Result").ValueKind == JsonValueKind.Null ? (decimal?)null : row.GetProperty("Result").GetDecimal())];
    }

    [Fact]
    public async Task ExecuteEmptyInputsFailsWithNoInputRows()
    {
        var type = new WindowedCalculationEntityType();

        var result = await type.Execute(EntityFor("Rank"), [], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21023");
        var advice = result.Messages[^1].ShouldBeAssignableTo<IClientMessage>().ClientText;
        advice.ShouldContain("DataSet input");
        advice.ShouldNotContain("Scalar");
    }

    [Fact]
    public async Task ExecuteWithoutTypedConfigurationFailsAsNotLoaded()
    {
        var type = new WindowedCalculationEntityType();
        var entity = new TestCalculationEntity { CalculationEntityType = "Windowed" };

        var result = await type.Execute(entity, [ScoreInput(1m)], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-61020");
    }

    [Fact]
    public async Task ExecuteWithAnUnofferedWindowFunctionFailsNamingTheOfferedOnes()
    {
        var type = new WindowedCalculationEntityType();

        var result = await type.Execute(EntityFor("Median"), [ScoreInput(1m)], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21020");
        result.Messages[^1].Message.ShouldContain("DenseRank");
    }

    [Fact]
    public async Task ExecuteWithATargetFieldNoRowCarriesFails()
    {
        var type = new WindowedCalculationEntityType();

        var result = await type.Execute(EntityFor("Rank", "Missing"), [ScoreInput(1m)], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21002");
    }

    [Fact]
    public async Task ExecuteReportsTheConfiguredFunctionAndOneRowPerInput()
    {
        var type = new WindowedCalculationEntityType();

        var result = await type.Execute(EntityFor("Sum"), [ScoreInput(10m, 20m, 30m)], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("WindowFunction").GetString().ShouldBe("Sum");
        doc.RootElement.GetProperty("PartitionCount").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(3);
    }

    [Theory]
    [InlineData("RowNumber", new[] { 1, 2, 3, 4 })]
    [InlineData("Rank", new[] { 1, 2, 2, 4 })]
    [InlineData("DenseRank", new[] { 1, 2, 2, 3 })]
    [InlineData("RunningTotal", new[] { 10, 30, 50, 80 })]
    [InlineData("Sum", new[] { 10, 30, 50, 80 })]
    [InlineData("Count", new[] { 1, 2, 3, 4 })]
    [InlineData("Min", new[] { 10, 10, 10, 10 })]
    [InlineData("Max", new[] { 10, 20, 20, 30 })]
    public async Task ExecuteComputesEachOfferedFunctionOverTheRows(string windowFunction, int[] expected)
    {
        var column = await RunColumn(windowFunction, 10m, 20m, 20m, 30m);

        column.ShouldBe(expected.Select(value => (decimal?)value).ToList());
    }

    [Fact]
    public async Task ExecuteAvgIsTheRunningAverage()
    {
        (await RunColumn("Avg", 10m, 20m, 30m)).ShouldBe([10m, 15m, 20m]);
    }

    [Fact]
    public async Task ExecuteLeadHasNoValueInTheLastRow()
    {
        (await RunColumn("Lead", 10m, 20m, 30m)).ShouldBe([20m, 30m, null]);
    }

    [Fact]
    public async Task ExecuteLagHasNoValueInTheFirstRow()
    {
        (await RunColumn("Lag", 10m, 20m, 30m)).ShouldBe([null, 10m, 20m]);
    }

    private static ICalculationContext ContextWith(object spec)
    {
        var context = new Mock<ICalculationContext>();
        context.SetupGet(c => c.Parameters)
            .Returns(new Dictionary<string, object?> { [WindowedCalculationSpec.ParameterName] = spec });
        return context.Object;
    }

    private static ResolvedCalculationInput TeamScores(params (string Team, decimal Score)[] rows)
        => DataSetInput("A", [.. rows.Select(row => new Dictionary<string, object> { ["Team"] = row.Team, ["Score"] = row.Score })]);

    private static async Task<JsonElement> RunSpec(WindowedCalculationSpec spec, params (string Team, decimal Score)[] rows)
    {
        var result = await new WindowedCalculationEntityType().Execute(
            EntityFor("Rank"), [TeamScores(rows)], ContextWith(spec), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        return JsonDocument.Parse(result.Value!).RootElement.Clone();
    }

    [Fact]
    public async Task ExecuteRunsTheSpecTheCallerSuppliesInsteadOfTheStoredConfiguration()
    {
        var root = await RunSpec(
            new WindowedCalculationSpec { TargetField = "Score", WindowFunction = "Sum", OutputFieldName = "Total" },
            ("A", 1m), ("A", 2m));

        root.GetProperty("WindowFunction").GetString().ShouldBe("Sum");
        root.GetProperty("ResultField").GetString().ShouldBe("Total");
        root.GetProperty("Rows").EnumerateArray().Select(row => row.GetProperty("Total").GetDecimal()).ShouldBe([1m, 3m]);
    }

    [Fact]
    public async Task ExecuteNumbersRowsWithinEachPartitionInTheOrderTheSpecStates()
    {
        var root = await RunSpec(
            new WindowedCalculationSpec
            {
                TargetField = "Score",
                WindowFunction = "RowNumber",
                OutputFieldName = "Place",
                PartitionByFields = ["Team"],
                OrderByFields = [new WindowOrderField { FieldName = "Score", Descending = true }]
            },
            ("A", 10m), ("B", 5m), ("A", 20m));

        root.GetProperty("PartitionCount").GetInt32().ShouldBe(2);
        root.GetProperty("Rows").EnumerateArray().Select(row => row.GetProperty("Place").GetDecimal()).ShouldBe([2m, 1m, 1m]);
    }

    [Fact]
    public async Task ExecuteOrdersTheWholeSetWhenThereIsNoPartition()
    {
        var root = await RunSpec(
            new WindowedCalculationSpec
            {
                TargetField = "Score",
                WindowFunction = "RowNumber",
                OutputFieldName = "Place",
                OrderByFields = [new WindowOrderField { FieldName = "Score", Descending = true }]
            },
            ("A", 10m), ("A", 30m), ("A", 20m));

        root.GetProperty("PartitionCount").GetInt32().ShouldBe(1);
        root.GetProperty("Rows").EnumerateArray().Select(row => row.GetProperty("Place").GetDecimal()).ShouldBe([3m, 1m, 2m]);
    }

    [Fact]
    public async Task ExecuteWithAPartitionFieldNoRowCarriesFails()
    {
        var spec = new WindowedCalculationSpec
        {
            TargetField = "Score", WindowFunction = "Rank", OutputFieldName = "Place", PartitionByFields = ["League"]
        };

        var result = await new WindowedCalculationEntityType().Execute(
            EntityFor("Rank"), [TeamScores(("A", 1m))], ContextWith(spec), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21002");
    }

    [Fact]
    public async Task ExecuteWithASuppliedSpecThatIsNotASpecFails()
    {
        var result = await new WindowedCalculationEntityType().Execute(
            EntityFor("Rank"), [TeamScores(("A", 1m))], ContextWith("not a spec"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21021");
    }

    [Fact]
    public async Task ExecuteAggregatesRowsFromMultipleInputs()
    {
        var type = new WindowedCalculationEntityType();
        var inputs = new List<ResolvedCalculationInput>
        {
            DataSetInput("A", new Dictionary<string, object> { ["Score"] = 1m }),
            DataSetInput("B", new Dictionary<string, object> { ["Score"] = 2m }, new Dictionary<string, object> { ["Score"] = 3m })
        };

        var result = await type.Execute(EntityFor("RowNumber"), inputs, Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteFailsWithNoInputRowsWhenNoInputResolvedValueIsRowData()
    {
        var type = new WindowedCalculationEntityType();
        var scalarInput = new ResolvedCalculationInput
        {
            InputAlias = "S",
            Kind = CalculationInputKinds.ByName("Scalar"),
            ResolvedValue = 42
        };

        var result = await type.Execute(EntityFor("Rank"), [scalarInput], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21023");
        var advice = result.Messages[^1].ShouldBeAssignableTo<IClientMessage>().ClientText;
        advice.ShouldContain("DataSet input");
        advice.ShouldNotContain("Scalar");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteCancelledTokenReturnsWindowedExecutionFailed()
    {
        var type = new WindowedCalculationEntityType();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await type.Execute(EntityFor("Rank"), [ScoreInput(1m)], Mock.Of<ICalculationContext>(), cts.Token);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-91004");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationEmptyTargetFieldReturnsFailure()
    {
        var type = new WindowedCalculationEntityType();
        var config = new WindowedCalculationConfiguration { TargetField = "", WindowFunction = "Rank" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("TargetField is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationEmptyWindowFunctionReturnsFailure()
    {
        var type = new WindowedCalculationEntityType();
        var config = new WindowedCalculationConfiguration { TargetField = "Score", WindowFunction = "" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
        result.CurrentMessage.ShouldContain("WindowFunction is required");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationValidReturnsSuccess()
    {
        var type = new WindowedCalculationEntityType();
        var config = new WindowedCalculationConfiguration { TargetField = "Score", WindowFunction = "Rank" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateConfigurationWrongTypeReturnsConfigurationTypeMismatch()
    {
        var type = new WindowedCalculationEntityType();

        var result = type.ValidateConfiguration(Mock.Of<IGenericConfiguration>());

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("ConfigurationTypeMismatch");
    }

    [Fact]
    public void ConfigurationTypeReturnsWindowedCalculationConfigurationType()
    {
        var type = new WindowedCalculationEntityType();

        type.ConfigurationType.ShouldBe(typeof(WindowedCalculationConfiguration));
    }

    [Fact]
    public void TypedContainerNameIsNullByDefault()
    {
        var type = new WindowedCalculationEntityType();

        type.TypedContainerName.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ValidateTypedConfigurationUnofferedWindowFunctionReturnsFailure()
    {
        var type = new WindowedCalculationEntityType();
        var config = new WindowedCalculationConfiguration { TargetField = "Score", WindowFunction = "Median" };

        var result = type.ValidateConfiguration(config);

        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("CALCULATIONS-21020");
    }
}
