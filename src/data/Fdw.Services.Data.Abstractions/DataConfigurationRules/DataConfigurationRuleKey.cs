using System;

namespace Fdw.Services.Data.Abstractions.DataConfigurationRules;

/// <summary>One rule of a container (FDW-6345).</summary>
public sealed class DataConfigurationRuleKey
{
    /// <summary>Gets the container the rule is listed under.</summary>
    public required DataConfigurationRuleContainer Target { get; init; }

    /// <summary>Gets the rule's durable id.</summary>
    public required Guid RuleId { get; init; }
}
