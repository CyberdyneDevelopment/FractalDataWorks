using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>
/// One rule of a threshold (FDW-3936): a condition on the values a subject reports, and the consequence that fires when it holds.
/// </summary>
/// <param name="Criteria">The condition, in the filter model: conditions of field, operator and value in AND/OR groups. It names only fields the threshold type reports.</param>
/// <param name="Escalation">The name of an <c>EscalationTypes</c> option: what fires.</param>
/// <param name="EscalationPolicy">The escalation policy the Alert consequence follows; required by a consequence that needs one, and none for the rest.</param>
/// <param name="NotificationRules">The notification rules the Notify consequence sends; empty for the rest.</param>
public sealed record ThresholdRule(IFilterNode Criteria, string Escalation, string? EscalationPolicy, IReadOnlyList<string> NotificationRules);
