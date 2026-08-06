using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Validation rule that can be displayed in UI and enforced server-side.
/// </summary>
public class ValidationRuleInfoDto
{
    /// <summary>
    /// Gets or sets the rule type (e.g., "Required", "MaxLength", "Pattern", "Range").
    /// </summary>
    public required string RuleType { get; set; }

    /// <summary>
    /// Gets or sets the user-friendly error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets rule-specific parameters (e.g., { "max": 100 }, { "pattern": "^[a-z]+$" }).
    /// </summary>
    public IDictionary<string, object>? Parameters { get; set; }
}
