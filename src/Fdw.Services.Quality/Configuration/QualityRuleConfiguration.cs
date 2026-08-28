using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Quality.Configuration;

/// <summary>
/// Configuration for quality validation rules.
/// Stored in quality.QualityRule table.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration( ServiceCategory = "Quality",
    ServiceType = "Rule")]
public sealed partial class QualityRuleConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public string SectionName => "Qualitys";

    /// <inheritdoc />
    public string ServiceType => "Quality";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>
    /// Gets or sets the display name for this quality rule.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for this rule.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the DataSet this rule applies to.
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field name this rule applies to (null for aggregate rules).
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Gets or sets the rule type (from QualityRuleTypes TypeCollection).
    /// </summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the severity level (from QualitySeverityTypes TypeCollection).
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Gets or sets whether this rule is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional description of this rule.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the minimum value for range validation.
    /// </summary>
    public string? MinValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for range validation.
    /// </summary>
    public string? MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the regex pattern for pattern validation.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the SQL expression for custom validation.
    /// </summary>
    public string? Expression { get; set; }

    /// <summary>
    /// Gets or sets whether failed rules should attempt automatic remediation.
    /// </summary>
    public bool AutoRemediate { get; set; }

    /// <summary>
    /// Gets or sets the reference values for this quality rule.
    /// </summary>
    public IList<QualityRuleReferenceValueConfiguration> ReferenceValues { get; set; } = [];
}
