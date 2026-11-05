using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Operations.Configuration;

/// <summary>A notification rule a rule's Notify consequence sends (FDW-3936). Child of ThresholdRuleConfiguration.</summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Operations")]
public sealed partial class ThresholdRuleNotificationConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the notification rule.</summary>
    public string NotificationRuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets the place in the list, counted from 1.</summary>
    public int Ordinal { get; set; }
}
