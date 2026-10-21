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
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

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
        var result = PipelineTransformConfigurationMapper.Map(
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
        var result = PipelineTransformConfigurationMapper.Map([Validate()], NullLogger.Instance);

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
        var result = PipelineTransformConfigurationMapper.Map(
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
        var result = PipelineTransformConfigurationMapper.Map([Filter(expression: "Age >")], NullLogger.Instance);

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
        var result = PipelineTransformConfigurationMapper.Map([Filter(name)], NullLogger.Instance);

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
        var result = PipelineTransformConfigurationMapper.Map([Filter("Keep"), Filter("keep", "Age < 99")], NullLogger.Instance);

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
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

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
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

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

        // Act
        var result = PipelineTransformConfigurationMapper.Map([request], NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
    }
}
