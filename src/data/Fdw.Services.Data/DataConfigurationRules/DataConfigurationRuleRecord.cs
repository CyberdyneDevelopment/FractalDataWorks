using System;
using Fdw.Data;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>
/// A data configuration rule as the save writes it (FDW-6345): the fields the rule table holds; the database provides the versioning
/// columns. Its mapper is generated, and the container declares Id as its Logical key, so a change replaces the current row of the Id.
/// </summary>
[GenerateMapper]
public sealed class DataConfigurationRuleRecord
{
    /// <summary>Gets or sets the rule's durable id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the rule's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule's abbreviation.</summary>
    public string Abbreviation { get; set; } = string.Empty;

    /// <summary>Gets or sets what the rule is for.</summary>
    // Why [NotStored]: the description is kept in the descriptions table, in the language of the caller's tenant (FDW-6208), so the column is gone and no save writes it; the service writes
    // it in the unit of the save and fills the answer from the table.
    [NotStored]
    public string? Description { get; set; }

    /// <summary>Gets or sets the data store the rule is about.</summary>
    public string DataStore { get; set; } = string.Empty;

    /// <summary>Gets or sets the path of the data store.</summary>
    public string DataPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the container the rule is about.</summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>Gets or sets the column the rule is about.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the path and method as one text, such as <c>DataConfigurationRules:Comparison:In.To</c>.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of owner.</summary>
    public string OwnerKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning organization or workspace.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Gets or sets the custodian.</summary>
    public Guid CustodianUserId { get; set; }

    /// <summary>Gets or sets the permission a person other than the custodian must hold.</summary>
    public string? RequiredPermission { get; set; }

    /// <summary>Gets or sets whether the rule is switched on.</summary>
    public bool Active { get; set; }
}
