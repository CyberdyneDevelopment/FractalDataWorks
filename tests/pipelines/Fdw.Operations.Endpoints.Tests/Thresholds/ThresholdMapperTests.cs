using System;
using System.Linq;
using System.Text.Json;
using Fdw.Operations.Endpoints.Thresholds;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.Thresholds;

/// <summary>
/// Tests for <see cref="ThresholdMapper"/> (FDW-3948): a threshold is stored as filter group and condition rows and read back as the same filter form, what the payload does not state is
/// refused by name, and a value the threshold type reports as a whole number is stored as an Int64.
/// </summary>
public sealed class ThresholdMapperTests
{
    private const string Type = "PipelineWaits";
    private const string Field = "waitingRuns";

    private static JsonElement Json(string text)
    {
        using var document = JsonDocument.Parse(text);
        return document.RootElement.Clone();
    }

    private static ThresholdPayload Payload(string criteria, bool? isEnabled = true, bool? firesOnEntry = false, string type = Type) => new()
    {
        Name = "queue-too-long",
        Type = type,
        IsEnabled = isEnabled,
        FiresOnEntry = firesOnEntry,
        Rules = [new ThresholdRulePayload { Criteria = Json(criteria), Escalation = "Alert", EscalationPolicy = "page-the-team" }],
    };

    private static string AtLeast(int runs) => $$"""{"logic":"And","nodes":[{"field":"{{Field}}","operator":"GreaterThanOrEqual","value":{{runs}}}]}""";

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AThresholdIsStoredAsRowsAndReadBackAsTheSameFilter()
    {
        var built = ThresholdMapper.ToConfiguration(Payload(AtLeast(5)), Guid.NewGuid());

        built.IsSuccess.ShouldBeTrue();
        var stored = built.Value.ShouldNotBeNull();
        stored.Implementation.ShouldBe(Type);
        stored.SubjectKind.ShouldBe("Pipeline");
        var rule = stored.Rules.ShouldHaveSingleItem();
        rule.Ordinal.ShouldBe(1);
        rule.Escalation.ShouldBe("Alert");
        rule.EscalationPolicy.ShouldBe("page-the-team");
        var root = rule.FilterGroups.ShouldHaveSingleItem();
        root.ParentGroupId.ShouldBeNull();
        root.Operator.ShouldBe("All");
        var condition = root.Conditions.ShouldHaveSingleItem();
        (condition.FieldName, condition.Operator, condition.Value).ShouldBe((Field, "GreaterThanOrEqual", "5"));

        var read = ThresholdMapper.ToPayload(stored);

        read.IsSuccess.ShouldBeTrue();
        var payload = read.Value.ShouldNotBeNull();
        payload.IsEnabled.ShouldBe(true);
        payload.FiresOnEntry.ShouldBe(false);
        var node = payload.Rules.ShouldHaveSingleItem().Criteria;
        node.GetProperty("logic").GetString().ShouldBe("And");
        var first = node.GetProperty("nodes").EnumerateArray().Single();
        first.GetProperty("field").GetString().ShouldBe(Field);
        first.GetProperty("value").GetInt64().ShouldBe(5);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AWholeNumberTheTypeReportsIsStoredAsInt64()
    {
        var stored = ThresholdMapper.ToConfiguration(Payload(AtLeast(5)), Guid.NewGuid()).Value.ShouldNotBeNull();

        stored.Rules.Single().FilterGroups.Single().Conditions.Single().DataType.ShouldBe("Int64");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AConditionThatStandsAloneIsWrappedInAnAllGroup()
    {
        var stored = ThresholdMapper.ToConfiguration(Payload($$"""{"field":"{{Field}}","operator":"LessThan","value":2}"""), Guid.NewGuid()).Value.ShouldNotBeNull();

        var root = stored.Rules.Single().FilterGroups.ShouldHaveSingleItem();
        root.Operator.ShouldBe("All");
        root.Conditions.ShouldHaveSingleItem().Operator.ShouldBe("LessThan");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ANestedGroupIsAChildOfItsParentAndReadsBackNested()
    {
        var criteria = $$"""{"logic":"Or","nodes":[{"field":"{{Field}}","operator":"Equal","value":1},{"logic":"And","nodes":[{"field":"{{Field}}","operator":"GreaterThan","value":9}]}]}""";

        var stored = ThresholdMapper.ToConfiguration(Payload(criteria), Guid.NewGuid()).Value.ShouldNotBeNull();

        var groups = stored.Rules.Single().FilterGroups;
        groups.Count.ShouldBe(2);
        var root = groups.Single(group => group.ParentGroupId is null);
        root.Operator.ShouldBe("Any");
        groups.Single(group => group.ParentGroupId == root.Id).Operator.ShouldBe("All");

        var nodes = ThresholdMapper.ToPayload(stored).Value.ShouldNotBeNull().Rules.Single().Criteria.GetProperty("nodes").EnumerateArray().ToList();
        nodes.Count.ShouldBe(2);
        nodes[0].GetProperty("field").GetString().ShouldBe(Field);
        nodes[1].GetProperty("logic").GetString().ShouldBe("And");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AnIsEnabledThatIsNotStatedIsRefused()
    {
        var built = ThresholdMapper.ToConfiguration(Payload(AtLeast(5), isEnabled: null), Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("isEnabled");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFiresOnEntryThatIsNotStatedIsRefused()
    {
        var built = ThresholdMapper.ToConfiguration(Payload(AtLeast(5), firesOnEntry: null), Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("firesOnEntry");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ATypeThatIsNotAThresholdTypeIsRefused()
    {
        var built = ThresholdMapper.ToConfiguration(Payload(AtLeast(5), type: "NoSuchType"), Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("NoSuchType");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AFieldTheTypeDoesNotReportIsRefused()
    {
        var built = ThresholdMapper.ToConfiguration(Payload("""{"field":"rowsOut","operator":"LessThan","value":2}"""), Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("rowsOut");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("Contains")]
    [InlineData("IsNull")]
    [InlineData("In")]
    public void AComparisonTheFieldDoesNotTakeIsRefusedNamingWhatItTakes(string comparison)
    {
        var built = ThresholdMapper.ToConfiguration(Payload($$"""{"field":"{{Field}}","operator":"{{comparison}}","value":2}"""), Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain(comparison);
        built.CurrentMessage.ShouldContain("GreaterThanOrEqual");
    }

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    [InlineData("[]", "filter object")]
    [InlineData("""{"logic":"Maybe","nodes":[]}""", "neither And nor Or")]
    [InlineData("""{"operator":"LessThan","value":2}""", "names no field")]
    [InlineData("""{"field":"waitingRuns","value":2}""", "names no operator")]
    public void CriteriaThatAreNotAFilterAreRefusedWithTheReason(string criteria, string reason)
    {
        var built = ThresholdMapper.ToConfiguration(Payload(criteria), Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain(reason);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void AParameterTheTypeDoesNotDeclareIsRefused()
    {
        var payload = Payload(AtLeast(5));
        payload.Parameters = [new ThresholdParameterPayload { Name = "field", Value = Json("\"Name\"") }];

        var built = ThresholdMapper.ToConfiguration(payload, Guid.NewGuid());

        built.IsFailure.ShouldBeTrue();
        built.CurrentMessage.ShouldNotBeNull().ShouldContain("declares no parameter 'field'");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ARuleKeepsItsNotificationRulesInOrder()
    {
        var payload = Payload(AtLeast(5));
        payload.Rules = [new ThresholdRulePayload { Criteria = Json(AtLeast(5)), Escalation = "Notify", NotificationRules = ["first", "second"] }];

        var stored = ThresholdMapper.ToConfiguration(payload, Guid.NewGuid()).Value.ShouldNotBeNull();

        stored.Rules.Single().Notifications.Select(notification => (notification.Ordinal, notification.NotificationRuleName)).ShouldBe(new[] { (1, "first"), (2, "second") });
        ThresholdMapper.ToPayload(stored).Value.ShouldNotBeNull().Rules.Single().NotificationRules.ShouldBe(new[] { "first", "second" });
    }
}
