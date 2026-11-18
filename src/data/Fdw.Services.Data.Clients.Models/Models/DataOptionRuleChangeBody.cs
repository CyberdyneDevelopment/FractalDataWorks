using System;
using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The body of a change to a data option rule (FDW-6339): a field left out keeps the stored one.</summary>
public sealed class DataOptionRuleChangeBody
{
    /// <summary>Gets or sets the rule's name.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the rule's abbreviation.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets what the rule is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the column the rule is about.</summary>
    public string? Column { get; set; }

    /// <summary>Gets or sets the path's names.</summary>
    public List<string>? Path { get; set; }

    /// <summary>Gets or sets the method the path ends in.</summary>
    public string? Method { get; set; }



    /// <summary>Gets or sets the organization or workspace the rule belongs to.</summary>
    public DataOptionOwnerBody? Owner { get; set; }

    /// <summary>Gets or sets the custodian.</summary>
    public Guid? CustodianUserId { get; set; }

    /// <summary>Gets or sets the permission a person other than the custodian must hold to change the rule.</summary>
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// Gets or sets the optional fields of the rule to clear, by their payload names (such as <c>description</c>). A field left out of
    /// the body keeps the stored value, so clearing is stated here and never by sending null. Naming a field that cannot be cleared is
    /// refused (FieldNotClearable).
    /// </summary>
    public List<string>? Clear { get; set; }
}
