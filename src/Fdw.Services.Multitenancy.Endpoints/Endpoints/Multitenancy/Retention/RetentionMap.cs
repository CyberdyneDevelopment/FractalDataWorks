using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Multitenancy.Abstractions;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// Maps retention policies, legal holds and run reports to the responses the endpoints return (FDW-1575).
/// </summary>
internal static class RetentionMap
{
    internal static RetentionPolicyResponse Of(RetentionPolicy policy) => new()
    {
        Id = policy.Id, DocumentType = policy.DocumentType, Years = policy.Years, Months = policy.Months, Days = policy.Days,
        StartField = policy.StartField, EndAction = policy.EndAction,
    };

    internal static RetentionPoliciesResponse Of(IReadOnlyList<RetentionPolicy> policies) => new() { Policies = [.. policies.Select(policy => Of(policy))] };

    internal static LegalHoldResponse Of(LegalHold hold) => new()
    {
        Id = hold.Id, DocumentId = hold.DocumentId, Reason = hold.Reason, PlacedBy = hold.PlacedBy, PlacedAt = hold.PlacedAt,
        LiftedBy = hold.LiftedBy, LiftedAt = hold.LiftedAt, IsHeld = hold.IsHeld,
    };

    internal static LegalHoldsResponse Of(IReadOnlyList<LegalHold> holds) => new() { Holds = [.. holds.Select(hold => Of(hold))] };

    internal static RetentionRunResponse Of(RetentionRunReport report) => new()
    {
        RanAt = report.RanAt, Considered = report.Considered, Held = report.Held, Undated = report.Undated,
        Ended = [.. report.Ended.Select(outcome => Of(outcome))], Failed = [.. report.Failed.Select(outcome => Of(outcome))],
    };

    private static RetentionOutcomeResponse Of(RetentionOutcome outcome) => new()
    {
        DocumentId = outcome.DocumentId, DocumentType = outcome.DocumentType, EndAction = outcome.EndAction, Reason = outcome.Reason,
    };
}
