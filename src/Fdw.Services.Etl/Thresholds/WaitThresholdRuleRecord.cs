using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>A row of <c>pipe.WaitThresholdRule</c>: a notification rule a threshold sends when it fires (FDW-3161).</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public sealed partial class WaitThresholdRuleRecord
{
    /// <summary>Gets or sets the row's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the id of the threshold the rule belongs to.</summary>
    public Guid WaitThresholdId { get; set; }

    /// <summary>Gets or sets the name of the notification rule.</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the position of the rule among the threshold's rules.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether the row is the current version.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row is deleted.</summary>
    public bool IsDeleted { get; set; }
}
