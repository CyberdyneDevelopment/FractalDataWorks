using System;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Operations.Thresholds;

/// <summary>
/// Remembers which rules of which subjects are holding (FDW-3936), so a threshold that fires on entry fires when its criteria start to hold and not again until they have stopped.
/// One per host process: after a restart a rule that still holds fires once more, and the consequences that matter (an alert) are single already.
/// </summary>
public sealed class ThresholdEntryTracker
{
    private readonly Lock _gate = new();
    private readonly HashSet<string> _holding = new(StringComparer.Ordinal);

    /// <summary>Records that a rule's criteria hold.</summary>
    /// <param name="key">The rule, from <see cref="KeyOf"/>.</param>
    /// <returns>True when they did not hold at the last report, so the rule is entering; false when it was already holding.</returns>
    public bool Entered(string key)
    {
        lock (_gate)
        {
            return _holding.Add(key);
        }
    }

    /// <summary>Records that a rule's criteria do not hold.</summary>
    /// <param name="key">The rule, from <see cref="KeyOf"/>.</param>
    public void Left(string key)
    {
        lock (_gate)
        {
            _holding.Remove(key);
        }
    }

    /// <summary>Names a rule of a threshold for one subject.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <param name="kind">The subject kind.</param>
    /// <param name="subjectId">The subject.</param>
    /// <param name="threshold">The threshold.</param>
    /// <param name="rule">The rule, counted from 1.</param>
    /// <returns>The key.</returns>
    public static string KeyOf(Guid tenantId, string kind, Guid subjectId, string threshold, int rule) =>
        string.Join('|', tenantId, kind, subjectId, threshold, rule);
}
