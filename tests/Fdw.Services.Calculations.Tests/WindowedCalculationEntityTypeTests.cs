using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Calculations;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Tests.TestSupport;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Calculations.Tests;

/// <summary>
/// Covers <see cref="WindowedCalculationEntityType"/>: typed-configuration validation, row
/// aggregation across inputs, and the sealed <see cref="CalculationEntityBase{TConfiguration}"/>
/// dispatch. The window-function/target-field/partition/order-by wiring is a documented stub in the
/// production code (hardcoded to "Rank" with no partition/order fields read from configuration) —
/// see the tests marked "(defect)" below, which characterize that current behavior without
/// endorsing it.
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

    [Fact]
    public async Task ExecuteEmptyInputsReturnsSuccessWithZeroRows()
    {
        var type = new WindowedCalculationEntityType();
        var entity = new TestCalculationEntity { CalculationEntityType = "Windowed" };

        var result = await type.Execute(entity, [], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteWithRowsAppliesHardcodedRankRegardlessOfConfiguration()
    {
        var type = new WindowedCalculationEntityType();
        var entity = new TestCalculationEntity
        {
            CalculationEntityType = "Windowed",
            TypedConfiguration = new WindowedCalculationConfiguration
            {
                TargetField = "Score",
                WindowFunction = "Sum",
                OutputFieldName = "Rank"
            }
        };
        var inputs = new List<ResolvedCalculationInput>
        {
            DataSetInput("A",
                new Dictionary<string, object> { ["Score"] = 10m },
                new Dictionary<string, object> { ["Score"] = 20m },
                new Dictionary<string, object> { ["Score"] = 30m })
        };

        var result = await type.Execute(entity, inputs, Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("WindowFunction").GetString().ShouldBe("Rank");
        doc.RootElement.GetProperty("PartitionCount").GetInt32().ShouldBe(1);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteAggregatesRowsFromMultipleInputs()
    {
        var type = new WindowedCalculationEntityType();
        var entity = new TestCalculationEntity { CalculationEntityType = "Windowed" };
        var inputs = new List<ResolvedCalculationInput>
        {
            DataSetInput("A", new Dictionary<string, object> { ["X"] = 1m }),
            DataSetInput("B", new Dictionary<string, object> { ["X"] = 2m }, new Dictionary<string, object> { ["X"] = 3m })
        };

        var result = await type.Execute(entity, inputs, Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteIgnoresInputsWhoseResolvedValueIsNotRowData()
    {
        var type = new WindowedCalculationEntityType();
        var entity = new TestCalculationEntity { CalculationEntityType = "Windowed" };
        var scalarInput = new ResolvedCalculationInput
        {
            InputAlias = "S",
            Kind = CalculationInputKinds.ByName("Scalar"),
            ResolvedValue = 42
        };

        var result = await type.Execute(entity, [scalarInput], Mock.Of<ICalculationContext>(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        using var doc = JsonDocument.Parse(result.Value!);
        doc.RootElement.GetProperty("RowCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteCancelledTokenReturnsWindowedExecutionFailed()
    {
        var type = new WindowedCalculationEntityType();
        var entity = new TestCalculationEntity { CalculationEntityType = "Windowed" };
        var inputs = new List<ResolvedCalculationInput> { DataSetInput("A", new Dictionary<string, object> { ["X"] = 1m }) };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await type.Execute(entity, inputs, Mock.Of<ICalculationContext>(), cts.Token);

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
}
