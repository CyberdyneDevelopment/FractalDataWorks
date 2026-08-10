using System;
using System.Collections.Generic;
using Fdw.Data;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Database record representing a quality rule definition.</summary>
[GenerateMapper]
public partial class QualityRuleRecord
{
    /// <summary>Gets or sets the unique identifier of the quality rule.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of the DataSet this rule applies to.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the specific field name this rule validates.</summary>
    public string? FieldName { get; set; }

    /// <summary>Gets or sets the type of quality rule.</summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Gets or sets the severity level of the rule.</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the rule is enabled.</summary>
    public bool IsEnabled { get; set; }

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

    /// <summary>Gets or sets whether this is the current active version of the record.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Gets or sets whether this record has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    // Why: Column names match quality.QualityRule DDL (CreateDate/ModifyDate), not the
    // "At" alias convention. DataGateway maps by exact property-to-column name.
    /// <summary>Gets or sets the date and time the record was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the date and time the record was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }
}