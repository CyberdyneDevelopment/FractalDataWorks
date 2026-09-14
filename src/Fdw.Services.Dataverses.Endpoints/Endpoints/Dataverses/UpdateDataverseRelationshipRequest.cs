using System;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Sets or changes the join fields on an already-declared relationship.</summary>
public class UpdateDataverseRelationshipRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the relationship being changed.</summary>
    public Guid RelationshipId { get; set; }

    /// <summary>Gets or sets the left join field, or null to clear it.</summary>
    public Guid? LeftFieldId { get; set; }

    /// <summary>Gets or sets the right join field, or null to clear it.</summary>
    public Guid? RightFieldId { get; set; }
}
