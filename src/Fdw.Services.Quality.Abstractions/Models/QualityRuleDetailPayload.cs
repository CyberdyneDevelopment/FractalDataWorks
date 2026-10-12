using System;

namespace Fdw.Services.Quality.Clients.Models;

/// <summary>
/// Quality rule detail.
/// </summary>
public sealed class QualityRuleDetailPayload
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the rule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of rule.</summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule expression.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the rule is enabled.</summary>
    public bool IsEnabled { get; set; }
}
