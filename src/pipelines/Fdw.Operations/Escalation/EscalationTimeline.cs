using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Operations.Abstractions.Escalation;

namespace Fdw.Operations.Escalation;

/// <summary>
/// The timing rules of an escalation policy (FDW-3863), apart from any clock, store or notifier so they can be judged on their own.
/// </summary>
/// <remarks>
/// A level is due when the time the alert reached its previous level (when it fired, for the first) plus that level's own delay has passed. Levels are taken in the order of
/// their numbers, one at a time, and never past the policy's maximum level. A policy that is not enabled has no due level. Nothing is assumed: a policy with no level has none
/// to send, and the caller says so.
/// </remarks>
public static class EscalationTimeline
{
    /// <summary>Finds the next level an open alert is due to reach.</summary>
    /// <param name="policy">The policy the alert follows.</param>
    /// <param name="currentLevel">The highest level sent so far; 0 when none has been.</param>
    /// <param name="levelReachedAt">When that level was sent; when the alert fired while none has been.</param>
    /// <param name="now">The moment judged.</param>
    /// <returns>The level that is due now; none when the next one is not yet due, the policy is not enabled, or no level is left.</returns>
    public static EscalationStep? Due(IEscalationPolicy policy, int currentLevel, DateTimeOffset levelReachedAt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var next = Next(policy, currentLevel);
        return next is not null && policy.IsEnabled && now >= levelReachedAt.AddMinutes(next.DelayMinutes)
            ? new EscalationStep(next.Level, next)
            : null;
    }

    /// <summary>Finds the level that follows <paramref name="currentLevel"/>, due or not.</summary>
    /// <param name="policy">The policy.</param>
    /// <param name="currentLevel">The highest level sent so far; 0 when none has been.</param>
    /// <returns>The next level by number that is not past the policy's maximum; none when there is none.</returns>
    public static IEscalationLevel? Next(IEscalationPolicy policy, int currentLevel)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy.Levels
            .Where(level => level.Level > currentLevel && level.Level <= policy.MaxEscalationLevel)
            .OrderBy(level => level.Level)
            .FirstOrDefault();
    }

    /// <summary>Whether a threshold that fires may open a new alert.</summary>
    /// <param name="policy">The policy the new alert would follow.</param>
    /// <param name="lastResolvedAt">When the threshold's latest alert was resolved; none when it has none.</param>
    /// <param name="now">The moment judged.</param>
    /// <returns>True when the policy's cooldown has passed since the last resolution, or there was none.</returns>
    public static bool CooldownPassed(IEscalationPolicy policy, DateTimeOffset? lastResolvedAt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return lastResolvedAt is not { } resolved || now >= resolved.AddMinutes(policy.CooldownMinutes);
    }
}
