using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One recorded change of a data option rule (FDW-6339): who, when, what kind and which members.</summary>
public sealed class DataOptionRuleChangePayload
{
    /// <summary>Gets or sets when the change was made.</summary>
    public DateTimeOffset At { get; set; }

    /// <summary>Gets or sets who made the change.</summary>
    public Guid ByUserId { get; set; }

    /// <summary>Gets or sets the name of who made the change.</summary>
    public string ByName { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule that changed.</summary>
    public Guid RuleId { get; set; }

    /// <summary>Gets or sets the rule's name at the change.</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of change: Created, Changed, Removed, Restored, SwitchedOn or SwitchedOff (a DataOptionChangeKinds name).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind's display name, from the DataOptionChangeKinds collection.</summary>
    public string KindDisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets each member a Changed change changed, with its old and new value.</summary>
    public List<DataOptionMemberChangePayload> Changes { get; set; } = [];
}
