using System;
using System.Collections.Generic;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for <see cref="PipelineTransformConfigurationMapper"/> — the FDW-owned endpoint-layer mapper
/// that dispatches <see cref="CreatePipelineTransformRequest"/> specs onto typed
/// <c>PipelineTransformConfiguration</c> aggregates via <c>TransformTypes</c> (FDW-556). Covers
/// per-operation-type dispatch (never a switch) and the create-time fail-loud gate for every combine
/// transform (Aggregate/Lookup/Calculate/Filter) plus unknown operation types.
/// </summary>
public sealed class PipelineTransformConfigurationMapperTests
{
    private static CreatePipelineTransformRequest FilterRequest(string name) =>
        new() { Name = name, OperationType = "Filter", FilterExpression = "B == 1" };

    // ── Aggregate dispatch ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToAggregateTransformTypeAndPopulatesGroupByAndAggregations()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest
        {
            Name = "Agg1",
            OperationType = "Aggregate",
            Aggregation = new AggregationRequest
            {
                GroupByFields = ["TeamId"],
                Aggregations = [new AggregationItemRequest { SourceField = "Points", Function = "Sum", OutputField = "TotalPoints" }],
            }
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var config = result.Value![0];
        config.GroupByFields.Count.ShouldBe(1);
        config.GroupByFields[0].FieldName.ShouldBe("TeamId");
        config.Aggregations.Count.ShouldBe(1);
        config.Aggregations[0].SourceField.ShouldBe("Points");
        config.Aggregations[0].AggregateFunction.ShouldBe("Sum");
        config.Aggregations[0].OutputField.ShouldBe("TotalPoints");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenAggregatePostedWithoutParams()
    {
        // Arrange — the whole point of FDW-556: a param-less Aggregate must be rejected at create time.
        var request = new CreatePipelineTransformRequest { Name = "Agg1", OperationType = "Aggregate" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Agg1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Aggregate 'Agg1' has no group-by fields and/or aggregations");
    }

    // ── Lookup dispatch ──────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToLookupTransformTypeAndPopulatesOneRowPerColumn()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest
        {
            Name = "Lookup1",
            OperationType = "Lookup",
            Lookup = new LookupRequest
            {
                LookupDataSet = "Devices",
                LookupKeyField = "Id",
                SourceKeyField = "DeviceId",
                LookupColumns = ["Name", "Region"],
                JoinType = "Left",
            }
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var config = result.Value![0];
        config.Lookups.Count.ShouldBe(2);
        config.Lookups[0].LookupValueField.ShouldBe("Name");
        config.Lookups[1].LookupValueField.ShouldBe("Region");
        config.Lookups[0].LookupDataSet.ShouldBe("Devices");
        config.Lookups[0].JoinType.ShouldBe("Left");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenLookupPostedWithoutParams()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest { Name = "Lookup1", OperationType = "Lookup" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Lookup1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Lookup 'Lookup1' missing connection/dataset/keys or lookup columns");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenLookupJoinTypeUnknown()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest
        {
            Name = "Lookup1",
            OperationType = "Lookup",
            Lookup = new LookupRequest
            {
                LookupDataSet = "Devices",
                LookupKeyField = "Id",
                SourceKeyField = "DeviceId",
                LookupColumns = ["Name"],
                JoinType = "FullOuter",
            }
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Lookup1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Unknown lookup join type 'FullOuter' on 'Lookup1'");
    }

    // ── Calculate dispatch ───────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToCalculateTransformTypeAndPopulatesComputedColumns()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest
        {
            Name = "Calc1",
            OperationType = "Calculate",
            Calculation = new CalculationRequest
            {
                ComputedColumns = [new ComputedColumnRequest { OutputField = "Double", Formula = "Age * 2" }],
            }
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var config = result.Value![0];
        config.Calculations.Count.ShouldBe(1);
        config.Calculations[0].OutputField.ShouldBe("Double");
        config.Calculations[0].Expression.ShouldBe("Age * 2");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenCalculatePostedWithoutParams()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest { Name = "Calc1", OperationType = "Calculate" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Calc1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Calculate 'Calc1' has no computed columns");
    }

    // ── Filter dispatch ──────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToFilterTransformTypeAndPopulatesFilterExpression()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest { Name = "Filter1", OperationType = "Filter", FilterExpression = "Age >= 18" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value![0].FilterExpression.ShouldBe("Age >= 18");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenFilterPostedWithoutExpression()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest { Name = "Filter1", OperationType = "Filter" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Filter1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Filter 'Filter1' has no filter expression");
    }

    // ── Unknown operation type / multi-step ─────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenOperationTypeUnknown()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest { Name = "Weird1", OperationType = "Median" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Weird1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Unknown transform type 'Median'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ARefusedStepShowsItsCallerWhichStepAndWhy()
    {
        var request = new CreatePipelineTransformRequest { Name = "Weird1", OperationType = "Median" };

        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        result.Code!.FormatClientMessage(result.Details)
            .ShouldBe("Pipeline step 'Weird1' is invalid: Unknown transform type 'Median'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapProcessesMultipleStepsInOrderAndAssignsDistinctIds()
    {
        // Arrange
        var requests = new List<CreatePipelineTransformRequest>
        {
            FilterRequest("Filter0"),
            FilterRequest("Filter1"),
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map(requests, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value[0].OperationType.ShouldBe("Filter");
        result.Value[1].OperationType.ShouldBe("Filter");
        result.Value[0].Id.ShouldNotBe(result.Value[1].Id);
        result.Value[0].Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapStopsAtFirstFailureAndDoesNotProcessLaterSteps()
    {
        // Arrange — second step is a valid Filter, but the first (Aggregate, param-less) must short-circuit.
        var requests = new List<CreatePipelineTransformRequest>
        {
            new CreatePipelineTransformRequest { Name = "Agg1", OperationType = "Aggregate" },
            FilterRequest("Filter0"),
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map(requests, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Agg1");
        result.Details!.GetValue<string>("Reason").ShouldBe("Aggregate 'Agg1' has no group-by fields and/or aggregations");
    }

    // ── IsEnabled ────────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapCarriesAStepThatIsDisabledInsteadOfEnablingIt()
    {
        // Arrange
        var request = FilterRequest("Filter0");
        request.IsEnabled = false;

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value![0].IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapEnablesAStepThatDoesNotSay()
    {
        // Arrange
        var request = FilterRequest("Filter0");

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value![0].IsEnabled.ShouldBeTrue();
    }
}
