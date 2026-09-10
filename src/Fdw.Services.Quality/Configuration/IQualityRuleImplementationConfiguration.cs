using System;
using Fdw.Configuration;
using Fdw.Data;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Quality.Configuration;

/// <summary>The contract every QualityRule implementation carries.</summary>
public interface IQualityRuleImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid QualityRuleId { get; set; }

    /// <summary>Gets or sets the QualityRule DataSetName.</summary>
    string DataSetName { get; set; }

    /// <summary>Gets or sets the QualityRule CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the QualityRule FieldName.</summary>
    string? FieldName { get; set; }

    /// <summary>Gets or sets the QualityRule RuleType.</summary>
    string RuleType { get; set; }

    /// <summary>Gets or sets the QualityRule Severity.</summary>
    string Severity { get; set; }

    /// <summary>Gets or sets the QualityRule IsEnabled.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets the QualityRule Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the QualityRule MinValue.</summary>
    string? MinValue { get; set; }

    /// <summary>Gets or sets the QualityRule MaxValue.</summary>
    string? MaxValue { get; set; }

    /// <summary>Gets or sets the QualityRule Pattern.</summary>
    string? Pattern { get; set; }

    /// <summary>Gets or sets the QualityRule Expression.</summary>
    string? Expression { get; set; }

    /// <summary>Gets or sets the QualityRule AutoRemediate.</summary>
    bool AutoRemediate { get; set; }

    /// <summary>Gets or sets the QualityRule ReferenceValues.</summary>
    IList<QualityRuleReferenceValueConfiguration> ReferenceValues { get; set; }
}
