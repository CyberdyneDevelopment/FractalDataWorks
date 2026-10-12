using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Validation rule for a configuration property.
/// </summary>
public sealed class ValidationRuleInfo
{
    /// <summary>Gets or sets the rule type (e.g., "Required", "MaxLength", "Pattern", "Range").</summary>
    public string RuleType { get; set; } = string.Empty;
    /// <summary>Gets or sets the user-friendly error message.</summary>
    public string? Message { get; set; }
    /// <summary>Gets or sets rule-specific parameters.</summary>
    public IDictionary<string, object>? Parameters { get; set; }
}
