using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>
/// One rule of a threshold (FDW-3936): a criteria over the reported values and the consequence that fires when it holds. Child of ThresholdImplementationConfiguration. The
/// criteria are its filter groups (a tree with one root) and the notification rules a Notify consequence sends are its notifications; both are child rows.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Operations")]
public sealed partial class ThresholdRuleConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the place of the rule in the threshold, counted from 1. Rules are judged in this order.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the consequence: the name of an option of EscalationTypes.</summary>
    public string Escalation { get; set; } = string.Empty;

    /// <summary>Gets or sets the escalation policy an Alert consequence raises its alert on; none for a consequence that needs none.</summary>
    public string? EscalationPolicy { get; set; }

    /// <summary>Gets or sets every group of the criteria tree, flat: the one with no ParentGroupId is the root.</summary>
    public IList<ThresholdFilterGroupConfiguration> FilterGroups { get; set; } = [];

    /// <summary>Gets or sets the notification rules a Notify consequence sends, in order.</summary>
    public IList<ThresholdRuleNotificationConfiguration> Notifications { get; set; } = [];
}
