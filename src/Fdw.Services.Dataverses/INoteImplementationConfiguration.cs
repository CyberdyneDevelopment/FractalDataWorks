using Fdw.Configuration;
using Fdw.Data;
using System;

namespace Fdw.Services.Dataverses;

/// <summary>The contract every Note implementation carries.</summary>
public interface INoteImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the Note SubjectType.</summary>
    string SubjectType { get; set; }

    /// <summary>Gets or sets the Note SubjectId.</summary>
    Guid SubjectId { get; set; }

    /// <summary>Gets or sets the Note DataverseId.</summary>
    Guid? DataverseId { get; set; }

    /// <summary>Gets or sets the Note Body.</summary>
    string Body { get; set; }

    /// <summary>Gets or sets the Note AuthorUserId.</summary>
    Guid AuthorUserId { get; set; }

    /// <summary>Gets or sets the Note PromotedToRequestId.</summary>
    Guid? PromotedToRequestId { get; set; }

    /// <summary>Gets or sets the Note TenantId.</summary>
    Guid? TenantId { get; set; }

    /// <summary>Gets or sets the Note VisibilityGroupId.</summary>
    Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets the Note IsCurrent.</summary>
    bool IsCurrent { get; set; }

    /// <summary>Gets or sets the Note IsDeleted.</summary>
    bool IsDeleted { get; set; }

    /// <summary>Gets or sets the Note SrcCreateDate.</summary>
    DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the Note CreateDate.</summary>
    DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the Note CreateBy.</summary>
    string CreateBy { get; set; }

    /// <summary>Gets or sets the Note CreateOnBehalfOf.</summary>
    string CreateOnBehalfOf { get; set; }

    /// <summary>Gets or sets the Note ModifyDate.</summary>
    DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the Note ModifyBy.</summary>
    string ModifyBy { get; set; }

    /// <summary>Gets or sets the Note ModifyOnBehalfOf.</summary>
    string ModifyOnBehalfOf { get; set; }
}
