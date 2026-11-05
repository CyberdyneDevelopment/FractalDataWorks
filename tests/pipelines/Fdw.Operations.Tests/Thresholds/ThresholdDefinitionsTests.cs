using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Operations.Configuration;
using Fdw.Operations.Thresholds;

namespace Fdw.Operations.Tests.Thresholds;

/// <summary>
/// Tests for reading a stored threshold (FDW-3936). The rule checks (unknown field, unknown consequence, an Alert without a policy, a Notify without rules) run after the type
/// resolves, and this project defines no threshold type, so they are proved in the first stories that add one (FDW-3864, FDW-3865).
/// </summary>
public sealed class ThresholdDefinitionsTests
{
    private static ThresholdRuleConfiguration Rule(int ordinal, string escalation, string? policy = null) =>
        new()
        {
            Id = Guid.NewGuid(), Ordinal = ordinal, Escalation = escalation, EscalationPolicy = policy,
            FilterGroups =
            [
                new ThresholdFilterGroupConfiguration
                {
                    Id = Guid.NewGuid(), Operator = "All", Ordinal = 1,
                    Conditions = [new ThresholdFilterConditionConfiguration { Id = Guid.NewGuid(), FieldName = "consecutiveFailures", Operator = "GreaterThanOrEqual", Value = "3", DataType = "Int64", Ordinal = 1 }],
                },
            ],
        };

    private static ThresholdImplementationConfiguration Stored(string implementation, string kind, params ThresholdRuleConfiguration[] rules) =>
        new() { Id = Guid.NewGuid(), Name = "FailureRule", Domain = "Threshold", Implementation = implementation, SubjectKind = kind, IsEnabled = true, Rules = [.. rules] };

    private static ThresholdImplementationConfiguration WaitsWith(string op) => new()
    {
        Id = Guid.NewGuid(), Name = "Waits", Domain = "Threshold", Implementation = "ServerWaits", SubjectKind = "Server", IsEnabled = true,
        Rules =
        [
            new ThresholdRuleConfiguration
            {
                Id = Guid.NewGuid(), Ordinal = 1, Escalation = "Alert", EscalationPolicy = "Ops",
                FilterGroups =
                [
                    new ThresholdFilterGroupConfiguration
                    {
                        Id = Guid.NewGuid(), Operator = "All", Ordinal = 1,
                        Conditions = [new ThresholdFilterConditionConfiguration { Id = Guid.NewGuid(), FieldName = "waitingRuns", Operator = op, Value = "3", DataType = "Int64", Ordinal = 1 }],
                    },
                ],
            },
        ],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AConditionWhoseComparisonTheFieldDoesNotAcceptIsRefusedAndOneItAcceptsIsRead()
    {
        ThresholdDefinitions.From(WaitsWith("Contains")).IsFailure.ShouldBeTrue();
        ThresholdDefinitions.From(WaitsWith("IsNull")).IsFailure.ShouldBeTrue();
        ThresholdDefinitions.From(WaitsWith("GreaterThanOrEqual")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Etl")]
    public void AnUnknownThresholdTypeIsRefused()
    {
        ThresholdDefinitions.From(Stored("NoSuchType", "Pipeline", Rule(1, "Alert", "Ops"))).IsFailure.ShouldBeTrue();
    }
}
