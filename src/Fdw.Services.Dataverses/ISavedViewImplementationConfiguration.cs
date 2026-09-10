using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Dataverses;

/// <summary>The contract every SavedView implementation carries.</summary>
public interface ISavedViewImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the SavedView DisplayName.</summary>
    string? DisplayName { get; set; }

    /// <summary>Gets or sets the SavedView Description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the SavedView SubjectDataSetId.</summary>
    Guid SubjectDataSetId { get; set; }

    /// <summary>Gets or sets the SavedView Encoding.</summary>
    string Encoding { get; set; }

    /// <summary>Gets or sets the SavedView Filters.</summary>
    string? Filters { get; set; }

    /// <summary>Gets or sets the SavedView ChartType.</summary>
    string ChartType { get; set; }

    /// <summary>Gets or sets the SavedView OwnerUserId.</summary>
    Guid OwnerUserId { get; set; }

    /// <summary>Gets or sets the SavedView TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the SavedView VisibilityGroupId.</summary>
    Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the SavedView IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the SavedView IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the SavedView SrcCreateDate.</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the SavedView CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the SavedView CreateBy.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets or sets the SavedView CreateOnBehalfOf.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the SavedView ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the SavedView ModifyBy.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the SavedView ModifyOnBehalfOf.</summary>
    string ModifyOnBehalfOf { get; set; }
}
