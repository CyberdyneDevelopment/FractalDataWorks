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
/// transform (Aggregate/Lookup/Calculate/Filter) plus Map and unknown operation types.
/// </summary>
public sealed class PipelineTransformConfigurationMapperTests
{
    private static CreatePipelineTransformRequest MapRequest(params CreatePipelineFieldMappingRequest[] mappings) =>
        new()
        {
            Name = "Map1",
            OperationType = "Map",
            FieldMappings = [.. mappings],
        };

    // ── Map dispatch ─────────────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToMapTransformTypeAndPopulatesFieldMappings()
    {
        // Arrange
        var request = MapRequest(new CreatePipelineFieldMappingRequest { Name = "M1", SourceField = "A", DestinationField = "B" });

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].OperationType.ShouldBe("Map");
        result.Value[0].FieldMappings.Count.ShouldBe(1);
        result.Value[0].FieldMappings[0].SourceField.ShouldBe("A");
        result.Value[0].FieldMappings[0].DestinationField.ShouldBe("B");
        result.Value[0].FieldMappings[0].PipelineTransformId.ShouldBe(result.Value[0].Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapFailsLoudWhenFieldMappingsEmpty()
    {
        // Arrange
        var request = new CreatePipelineTransformRequest { Name = "Map1", OperationType = "Map" };

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11053");
    }

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
        result.Messages[^1].Code.ShouldBe("ETL-11045");
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
                LookupConnectionName = "Conn1",
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
        config.Lookups[0].LookupConnectionName.ShouldBe("Conn1");
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
        result.Messages[^1].Code.ShouldBe("ETL-11046");
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
                LookupConnectionName = "Conn1",
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
        result.Messages[^1].Code.ShouldBe("ETL-11054");
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
        result.Messages[^1].Code.ShouldBe("ETL-11047");
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
        result.Messages[^1].Code.ShouldBe("ETL-11048");
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
        result.Messages[^1].Code.ShouldBe("ETL-11049");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Etl")]
    public void MapProcessesMultipleStepsInOrderAndAssignsDistinctIds()
    {
        // Arrange
        var requests = new List<CreatePipelineTransformRequest>
        {
            MapRequest(new CreatePipelineFieldMappingRequest { Name = "M1", SourceField = "A", DestinationField = "B" }),
            new CreatePipelineTransformRequest { Name = "Filter1", OperationType = "Filter", FilterExpression = "B == 1" },
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map(requests, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Count.ShouldBe(2);
        result.Value[0].OperationType.ShouldBe("Map");
        result.Value[1].OperationType.ShouldBe("Filter");
        result.Value[0].Id.ShouldNotBe(result.Value[1].Id);
        result.Value[0].Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapStopsAtFirstFailureAndDoesNotProcessLaterSteps()
    {
        // Arrange — second step is a valid Map, but the first (Aggregate, param-less) must short-circuit.
        var requests = new List<CreatePipelineTransformRequest>
        {
            new CreatePipelineTransformRequest { Name = "Agg1", OperationType = "Aggregate" },
            MapRequest(new CreatePipelineFieldMappingRequest { Name = "M1", SourceField = "A", DestinationField = "B" }),
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map(requests, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages[^1].Code.ShouldBe("ETL-11045");
    }
}
