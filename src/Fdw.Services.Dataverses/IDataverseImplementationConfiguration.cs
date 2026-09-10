using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Dataverses;

/// <summary>The contract every Dataverse implementation carries.</summary>
public interface IDataverseImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid DataverseId { get; set; }

    /// <summary>Gets or sets the Dataverse DisplayName.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the Dataverse Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the Dataverse Purpose.</summary>
    string? Purpose { get; set; }

    /// <summary>Gets or sets the Dataverse Status.</summary>
    string Status { get; set; }

    /// <summary>Gets or sets the Dataverse Visibility.</summary>
    string Visibility { get; set; }

    /// <summary>Gets or sets the Dataverse JoinPolicy.</summary>
    string JoinPolicy { get; set; }

    /// <summary>Gets or sets the Dataverse OwnerUserId.</summary>
    Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets the Dataverse StandInSeed.</summary>
    string? StandInSeed { get; set; }

    /// <summary>Gets or sets the Dataverse TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the Dataverse VisibilityGroupId.</summary>
    Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the Dataverse Members.</summary>
    IList<DataverseMemberConfiguration> Members { get; set; }

    /// <summary>Gets or sets the Dataverse Resources.</summary>
    IList<DataverseResourceConfiguration> Resources { get; set; }

    /// <summary>Gets or sets the Dataverse Relationships.</summary>
    IList<DataverseRelationshipConfiguration> Relationships { get; set; }

    /// <summary>Gets or sets the Dataverse IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the Dataverse IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the Dataverse SrcCreateDate.</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the Dataverse CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the Dataverse CreateBy.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets or sets the Dataverse CreateOnBehalfOf.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the Dataverse ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the Dataverse ModifyBy.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the Dataverse ModifyOnBehalfOf.</summary>
    string ModifyOnBehalfOf { get; set; }
}
