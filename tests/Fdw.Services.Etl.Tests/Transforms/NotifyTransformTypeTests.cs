using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Transforms;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Tests for <see cref="NotifyTransformType"/> (FDW-1120): a stage that sends a notification through a rule at a run point,
/// leaves the records alone, and is refused rather than guessed when it does not say which rule, which point or what to say.
/// </summary>
public sealed class NotifyTransformTypeTests
{
    private readonly NotifyTransformType _sut = new();

    private static PipelineTransformConfiguration Target() => new() { Id = Guid.NewGuid(), Name = "Alert", OperationType = "Notify" };

    private static FakeTransformOperationSpec Spec(FakeNotificationSpec? notification) => new()
    {
        Name = "Alert",
        OperationType = "Notify",
        Notification = notification
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void NotifyIsATransformTypeOption()
    {
        TransformTypes.ByName("Notify").ShouldBeOfType<NotifyTransformType>();
        TransformTypes.All().ShouldContain(option => string.Equals(option.Name, "Notify", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void TheNotificationIsTheOneMemberAStageOfThisKindOwns()
    {
        _sut.Members.Select(member => member.Name).ShouldBe(new[] { "notification" });
        _sut.Members[0].IsRequired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public async Task TheRecordsPassThroughUntouched()
    {
        // Arrange
        var inputs = new List<IDictionary<string, object?>> { new Dictionary<string, object?> { ["A"] = 1 }, new Dictionary<string, object?> { ["A"] = 2 } };
        var context = new TransformContext(Guid.NewGuid(), NullLogger.Instance, new Dictionary<string, object?>(), fieldTransformChains: new Dictionary<Guid, IReadOnlyList<IFieldMappingTransformation>>());

        // Act
        var batch = await _sut.TransformBatch(inputs, Target(), context, TestContext.Current.CancellationToken);
        var single = await _sut.Transform(inputs[0], Target(), context, TestContext.Current.CancellationToken);

        // Assert
        batch.IsSuccess.ShouldBeTrue(batch.CurrentMessage);
        batch.Value!.ShouldBe(inputs);
        single.IsSuccess.ShouldBeTrue(single.CurrentMessage);
        single.Value.ShouldBeSameAs(inputs[0]);
        context.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void MapSpecToConfigurationCopiesTheRuleTheRunPointAndTheMessage()
    {
        // Arrange
        var target = Target();

        // Act
        var result = _sut.MapSpecToConfiguration(
            Spec(new FakeNotificationSpec("ops-alerts", "AfterLoad", "{{PipelineName}} loaded {{RowsLoaded}} rows")), target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        target.NotificationRuleName.ShouldBe("ops-alerts");
        target.RunPoint.ShouldBe("AfterLoad");
        target.NotificationMessage.ShouldBe("{{PipelineName}} loaded {{RowsLoaded}} rows");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    [InlineData("AfterStage")]
    [InlineData("AfterLoad")]
    [InlineData("OnFailure")]
    public void MapSpecToConfigurationAcceptsEveryRunPoint(string runPoint)
    {
        // Arrange
        var target = Target();

        // Act
        var result = _sut.MapSpecToConfiguration(Spec(new FakeNotificationSpec("ops-alerts", runPoint, "done")), target, NullLogger.Instance);

        // Assert
        result.IsSuccess.ShouldBeTrue(result.CurrentMessage);
        target.RunPoint.ShouldBe(runPoint);
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("", "AfterLoad", "done", "ruleName")]
    [InlineData("ops-alerts", "", "done", "runPoint")]
    [InlineData("ops-alerts", "Sometime", "done", "not a run point")]
    [InlineData("ops-alerts", "AfterLoad", "", "message")]
    [InlineData("ops-alerts", "AfterLoad", "  ", "message")]
    [InlineData("ops-alerts", "AfterLoad", "{{Nonsense}} happened", "unknown variables")]
    public void MapSpecToConfigurationRefusesAStageThatIsIncompleteAndStatesWhy(string rule, string runPoint, string message, string reason)
    {
        // Arrange
        var target = Target();

        // Act
        var result = _sut.MapSpecToConfiguration(Spec(new FakeNotificationSpec(rule, runPoint, message)), target, NullLogger.Instance);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
        target.RunPoint.ShouldBeNull();
        target.NotificationRuleName.ShouldBeNull();
        target.NotificationMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void MapSpecToConfigurationRefusesARequestThatStatesNoNotification()
    {
        // Act
        var result = _sut.MapSpecToConfiguration(Spec(null), Target(), NullLogger.Instance);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("does not state a notification");
    }
}
