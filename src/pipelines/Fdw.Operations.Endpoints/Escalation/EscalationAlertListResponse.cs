using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.Escalation;

/// <summary>The answer of the alerts list (FDW-3863).</summary>
public sealed class EscalationAlertListResponse
{
    /// <summary>Gets or sets the alerts of this page, newest first.</summary>
    public IReadOnlyList<EscalationAlertResponse> Items { get; set; } = [];

    /// <summary>Gets or sets how many alerts matched, before paging.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets how many were skipped.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets how many this page may hold; the number that matched when no page size was stated or set.</summary>
    public int Take { get; set; }

    /// <summary>
    /// Gets or sets how often, in seconds, the page reads the list again: the setting EscalationAlertsRefreshSeconds for the caller's scope. Null when the setting is not set or is
    /// not positive, and the page does not read again by itself.
    /// </summary>
    public int? RefreshSeconds { get; set; }
}
