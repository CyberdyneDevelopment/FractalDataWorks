using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The body of a data option rule to add (FDW-6339). Nothing has a default: a field a rule needs and the body leaves out is refused with its own code.</summary>
public sealed class NewDataOptionRuleBody
{
    /// <summary>Gets or sets the rule's name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the rule's abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets what the rule is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the data store the rule is based on; it must be the route's.</summary>
    public string? DataStore { get; set; }

    /// <summary>Gets or sets the path the rule is based on; it must be the route's.</summary>
    public string? DataPath { get; set; }

    /// <summary>Gets or sets the container the rule is based on; it must be the route's.</summary>
    public string? Container { get; set; }

    /// <summary>Gets or sets the column the rule is about.</summary>
    public string? Column { get; set; }

    /// <summary>Gets or sets the path's names, from DataConfigurationRules down to the leaf.</summary>
    public List<string>? Path { get; set; }

    /// <summary>Gets or sets the method the path ends in.</summary>
    public string? Method { get; set; }



    /// <summary>Gets or sets the organization or workspace the rule belongs to.</summary>
    public DataOptionOwnerBody? Owner { get; set; }

    /// <summary>Gets or sets the custodian.</summary>
    public Guid? CustodianUserId { get; set; }

    /// <summary>Gets or sets the permission a person other than the custodian must hold to change the rule; the caller must hold it.</summary>
    public string? RequiredPermission { get; set; }

    /// <summary>Gets or sets whether the rule starts switched on.</summary>
    public bool? Active { get; set; }

    /// <summary>Gets or sets the rule's values.</summary>
    public List<DataOptionValueBody>? Values { get; set; }
}
