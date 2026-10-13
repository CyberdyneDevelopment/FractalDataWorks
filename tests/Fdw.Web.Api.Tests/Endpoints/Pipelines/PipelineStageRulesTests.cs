using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Pipelines.Clients.Abstractions;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.Api.Tests.Endpoints.Pipelines;

/// <summary>
/// Tests for what a pipeline's stages must be before their kind reads them (FDW-1120, contract v5): the Validate stage
/// carries checks and a failing action, a stage name has no '.' and is used once, and a member that the stage's kind does
/// not own is refused, not ignored. An absent member and an empty list are the same thing.
/// </summary>
public sealed class PipelineStageRulesTests
{
    private static CreatePipelineTransformRequest Validate(string name = "Clean", string? onFail = "Reject", PipelineCheck[]? checks = null) =>
        new() { Name = name, OperationType = "Validate", OnFail = onFail, Checks = checks is null ? [] : [.. checks] };

    private static CreatePipelineTransformRequest Filter(string name = "Keep", string expression = "Age >= 18") =>
        new() { Name = name, OperationType = "Filter", FilterExpression = expression };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapDispatchesToValidateAndKeepsTheChecksInOrderCountedFromOne()
    {
        // Arrange
        var request = Validate(checks:
        [
            new PipelineCheck { Name = "Positive", Expression = "Points > 0" },
            new PipelineCheck { Name = "HasRegion", Expression = "Region != null" }
        ]);

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var stage = result.Value![0];
        stage.OnFail.ShouldBe("Reject");
        stage.Checks.Select(check => (check.Name, check.Expression, check.Ordinal))
            .ShouldBe(new[] { ("Positive", "Points > 0", 1), ("HasRegion", "Region != null", 2) });
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ignore")]
    public void AValidateStageThatDoesNotSayWhatAFailureCausesIsRefused(string? onFail)
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy",
            [Validate(onFail: onFail, checks: [new PipelineCheck { Name = "Positive", Expression = "Points > 0" }])], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Clean");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateStageWithNoChecksIsRefused()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Validate()], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("at least one check");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateCheckThatCannotBeReadIsRefusedNamingTheStageAndTheText()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy",
            [Validate(checks: [new PipelineCheck { Name = "Broken", Expression = "garbage expression" }])], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Clean");
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("garbage expression");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFilterConditionThatCannotBeReadIsRefusedWhenTheStageIsMapped()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Filter(expression: "Age >")], NullLogger.Instance);

        // Assert: it used to be saved and then pass every record at run time
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Keep");
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("Age >");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("clean.up")]
    [InlineData(".x")]
    [InlineData("x.")]
    public void AStageNameWithADotIsRefusedNamingIt(string name)
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Filter(name)], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe(name);
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("'.'");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void TwoStagesWithTheSameNameAreRefusedIgnoringCase()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [Filter("Keep"), Filter("keep", "Age < 99")], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("StepName").ShouldBe("keep");
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("more than one stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AMemberTheStageKindDoesNotOwnIsRefusedNamingIt()
    {
        // Arrange: a Filter that also states a sort
        var request = Filter();
        request.Sort = [new PipelineSortField { Field = "Age", Direction = "Ascending" }];

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("Reason").ShouldBe("'sort' is not a member of a Filter stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AValidateStageThatAlsoStatesAFilterExpressionIsRefused()
    {
        // Arrange
        var request = Validate(checks: [new PipelineCheck { Name = "Positive", Expression = "Points > 0" }]);
        request.FilterExpression = "Points > 0";

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("Reason").ShouldBe("'filterExpression' is not a member of a Validate stage");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnEmptyListOrAnAbsentMemberIsNotStated()
    {
        // Arrange: empty lists on a Filter stage count as nothing
        var request = Filter();
        request.Sort = [];
        request.Checks = [];
        request.FieldMappings = [];

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    private static CreatePipelineTransformRequest MapStage(IList<PipelineFieldTransformationReference>? chain) => new()
    {
        Name = "Shape",
        OperationType = "Map",
        FieldMappings = [new CreatePipelineFieldMappingRequest { Name = "A", SourceField = "a", DestinationField = "A", TransformationChain = chain }]
    };

    private static readonly Guid TrimId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UpperId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Dictionary<string, Guid> Known() =>
        new(StringComparer.OrdinalIgnoreCase) { ["Trim"] = TrimId, ["Upper"] = UpperId };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFieldMappingThatStatesATransformationChainStoresItByIdInOrder()
    {
        // Arrange: the second step is given by id, the first by name, stated out of order
        var chain = new List<PipelineFieldTransformationReference>
        {
            new() { TransformationId = UpperId, Ordinal = 2 },
            new() { Transformation = "trim", Ordinal = 1 },
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [MapStage(chain)], NullLogger.Instance, Known());

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var stored = result.Value![0].FieldMappings[0].TransformationChain.OrderBy(step => step.Ordinal).ToList();
        stored.Select(step => step.TransformationId).ShouldBe(new[] { TrimId, UpperId });
        stored.Select(step => step.Ordinal).ShouldBe(new[] { 1, 2 });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AChainStepNamingATransformationTheLookupDidNotFindIsRefusedNotDropped()
    {
        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy",
            [MapStage([new PipelineFieldTransformationReference { Transformation = "Missing", Ordinal = 1 }])], NullLogger.Instance, Known());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Shape");
        result.Details!.GetValue<string>("Reason").ShouldNotBeNull().ShouldContain("Missing");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("both")]
    [InlineData("neither")]
    [InlineData("gap")]
    public void AChainThatIsNotWellFormedIsRefused(string defect)
    {
        // Arrange
        var step = defect switch
        {
            "both" => new PipelineFieldTransformationReference { Transformation = "Trim", TransformationId = TrimId, Ordinal = 1 },
            "neither" => new PipelineFieldTransformationReference { Ordinal = 1 },
            _ => new PipelineFieldTransformationReference { Transformation = "Trim", Ordinal = 2 },
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [MapStage([step])], NullLogger.Instance, Known());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Code!.Id.ShouldBe(20001);
        result.Details!.GetValue<string>("StepName").ShouldBe("Shape");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    [InlineData(false)]
    [InlineData(true)]
    public void AFieldMappingWithNoChainIsAccepted(bool emptyList)
    {
        // Act: an absent chain and an empty one are the same thing
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [MapStage(emptyList ? [] : null)], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AChainOnAMapStageInsideABranchPathIsStoredOnTheNestedMapping()
    {
        // Arrange
        var inner = MapStage([new PipelineFieldTransformationReference { Transformation = "Trim", Ordinal = 1 }]);
        var branch = new CreatePipelineTransformRequest
        {
            Name = "Route",
            OperationType = "Branch",
            Unmatched = "Pass",
            Paths = [new CreatePipelinePathRequest { Name = "Adults", Condition = "Age >= 18", Stages = [inner] }],
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [branch], NullLogger.Instance, Known());

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        var nested = result.Value![0].Paths[0].Stages[0];
        nested.FieldMappings[0].TransformationChain.Select(step => step.TransformationId).ShouldBe(new[] { TrimId });
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnUnresolvedChainInsideABranchPathIsRefusedWithThePathToItsStage()
    {
        // Arrange
        var inner = MapStage([new PipelineFieldTransformationReference { Transformation = "Missing", Ordinal = 1 }]);
        var branch = new CreatePipelineTransformRequest
        {
            Name = "Route",
            OperationType = "Branch",
            Unmatched = "Pass",
            Paths = [new CreatePipelinePathRequest { Name = "Adults", Condition = "Age >= 18", Stages = [inner] }],
        };

        // Act
        var result = PipelineTransformConfigurationMapper.Map("BatchCopy", [branch], NullLogger.Instance, Known());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Details!.GetValue<string>("StepName").ShouldBe("Route.Adults.Shape");
    }
}
