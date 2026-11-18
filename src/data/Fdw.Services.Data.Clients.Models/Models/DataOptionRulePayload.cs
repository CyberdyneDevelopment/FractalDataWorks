using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>A data option rule (FDW-6339): what it is based on, the path it names, its values, who owns and controls it, and its state.</summary>
public sealed class DataOptionRulePayload
{
    /// <summary>Gets or sets the rule's id, by which it is read, changed, removed and restored.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the rule's name, unique among the current rules of its column.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule's abbreviation, unique among the current rules of its column.</summary>
    public string Abbreviation { get; set; } = string.Empty;

    /// <summary>Gets or sets what the rule is for: its intent.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data store the rule is based on.</summary>
    public string DataStore { get; set; } = string.Empty;

    /// <summary>Gets or sets the path of the data store the rule is based on.</summary>
    public string DataPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the container the rule is based on.</summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>Gets or sets the column the rule is about.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the names of the rule's path, one per level, from DataConfigurationRules down to the leaf.</summary>
    public List<string> Path { get; set; } = [];

    /// <summary>Gets or sets the method the path ends in (after the period), such as To.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule's values: one, or several for a leaf that takes a list.</summary>
    public List<DataOptionValuePayload> Values { get; set; } = [];



    /// <summary>Gets or sets the organization or workspace the rule belongs to.</summary>
    public required DataOptionOwnerPayload Owner { get; set; }

    /// <summary>Gets or sets the custodian: the person who controls the rule.</summary>
    public Guid CustodianUserId { get; set; }

    /// <summary>Gets or sets the custodian's name.</summary>
    public string CustodianName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the custodian still holds datastores:write: a name in the DataOptionCustodianStates collection, which the tree route answers (FDW-6346).</summary>
    public string CustodianState { get; set; } = string.Empty;

    /// <summary>Gets or sets the permission a person other than the custodian must hold to change the rule.</summary>
    public string? RequiredPermission { get; set; }

    /// <summary>Gets or sets whether the rule is switched on; a rule that is off is kept and does nothing.</summary>
    public bool Active { get; set; }

    /// <summary>Gets or sets whether the rule is removed; a removed rule is kept with its history and can be restored.</summary>
    public bool Removed { get; set; }

    /// <summary>Gets or sets when the rule was last changed (its current row's ModifyDate). A change is last writer wins; every version stays in history.</summary>
    public DateTimeOffset ChangedAt { get; set; }

    /// <summary>Gets or sets who last changed the rule; absent when no change of it is recorded (never the database login, never an empty text).</summary>
    public Guid? ChangedByUserId { get; set; }

    /// <summary>Gets or sets the name of who last changed the rule; absent exactly when <see cref="ChangedByUserId"/> is.</summary>
    public string? ChangedBy { get; set; }

    /// <summary>Gets or sets whether the caller may change the rule: the custodian, or a holder of its required permission.</summary>
    public bool CanChange { get; set; }
}
