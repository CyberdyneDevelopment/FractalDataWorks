using System;
using System.Collections.Generic;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Request containing the properties needed to create a new quality rule.</summary>
public class CreateQualityRuleRequest
{
    /// <summary>Gets or sets the rule name. When omitted, the server synthesizes "{dataSet}:{ruleType}".</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the name of the DataSet this rule applies to.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the specific field name this rule validates.</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the type of quality rule (e.g., NotNull, Unique, InRange, MatchesPattern).</summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level of the rule. Defaults to "Error".</summary>
    public string Severity { get; set; } = "Error";

    /// <summary>Gets or sets whether the rule is enabled. Defaults to true.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Gets or sets a human-readable description of the rule.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the minimum value for InRange rules.</summary>
    public string? MinValue { get; set; }

    /// <summary>Gets or sets the maximum value for InRange rules.</summary>
    public string? MaxValue { get; set; }

    /// <summary>Gets or sets the regex pattern for MatchesPattern rules.</summary>
    public string? Pattern { get; set; }

    /// <summary>Gets or sets the expression for CustomExpression rules.</summary>
    public string? Expression { get; set; }
}