using System;
using System.Collections.Generic;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>A dataverse with its members, resources and relationships.</summary>
public class DataverseDetailResponse
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the unique dataverse name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the question this project exists to answer.</summary>
    public string? Purpose { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets who can find this project.</summary>
    public string Visibility { get; set; } = string.Empty;

    /// <summary>Gets or sets what happens when someone asks to join.</summary>
    public string JoinPolicy { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning user.</summary>
    public Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets the project-wide seed for generated stand-in values.</summary>
    public string? StandInSeed { get; set; }

    /// <summary>Gets or sets the members.</summary>
    public IList<DataverseMemberDto> Members { get; set; } = [];

    /// <summary>Gets or sets the attached resources.</summary>
    public IList<DataverseResourceDto> Resources { get; set; } = [];

    /// <summary>Gets or sets the declared relationships.</summary>
    public IList<DataverseRelationshipDto> Relationships { get; set; } = [];

    /// <summary>Gets or sets when the project was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the project was last modified.</summary>
    public DateTimeOffset ModifiedAt { get; set; }
}
