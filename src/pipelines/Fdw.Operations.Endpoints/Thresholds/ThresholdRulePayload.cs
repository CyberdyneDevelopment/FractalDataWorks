using System.Collections.Generic;
using System.Text.Json;

namespace Fdw.Operations.Endpoints.Thresholds;

/// <summary>One rule of a threshold (FDW-3948): a criteria over the values its subject reports, and the consequence that fires when it holds.</summary>
public sealed class ThresholdRulePayload
{
    /// <summary>
    /// Gets or sets the criteria, in the filter form the data routes take: a group <c>{"logic":"And","nodes":[...]}</c> (or <c>"Or"</c>) of nodes, and <c>{"field":"rowsOut","operator":"LessThan","value":10}</c>
    /// for a condition. The field is one the threshold type reports; the operator takes one scalar value, or none.
    /// </summary>
    public JsonElement Criteria { get; set; }

    /// <summary>Gets or sets the consequence, the name of an escalation type.</summary>
    public string Escalation { get; set; } = string.Empty;

    /// <summary>Gets or sets the escalation policy a consequence that needs one raises its alert on; none for a consequence that needs none.</summary>
    public string? EscalationPolicy { get; set; }

    /// <summary>Gets or sets the notification rules a consequence that sends notifications uses; none for one that sends none.</summary>
    public IReadOnlyList<string> NotificationRules { get; set; } = [];
}
