using System;
using Fdw.Data;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>A value of a data configuration rule as the save writes it (FDW-6345); its mapper is generated and Id is the container's Logical key.</summary>
[GenerateMapper]
public sealed class DataConfigurationRuleValueRecord
{
    /// <summary>Gets or sets the value's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the id of the rule the value belongs to.</summary>
    public Guid DataConfigurationRuleId { get; set; }

    /// <summary>Gets or sets the kind, a name in the value kinds collection.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the literal.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the parameter's name.</summary>
    public string? Parameter { get; set; }

    /// <summary>Gets or sets the optional name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the optional abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    // Why [NotStored]: the description is kept in the descriptions table, in the language of the caller's tenant (FDW-6208), so the column is gone and no save writes it; the service writes
    // it in the unit of the save and fills the answer from the table.
    [NotStored]
    public string? Description { get; set; }

    /// <summary>Gets or sets the value's place among the rule's values, from 1.</summary>
    public int Ordinal { get; set; }
}
