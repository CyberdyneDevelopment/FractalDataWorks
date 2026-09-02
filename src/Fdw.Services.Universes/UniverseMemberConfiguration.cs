using System;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Universes;

/// <summary>
/// Maps to <c>universe.UniverseMember</c> — a person's membership of a universe.
/// </summary>
/// <remarks>
/// Why <see cref="State"/> is separate from <see cref="MemberRole"/>: an invitation that has not
/// been accepted is not membership, and the two answer different questions — what they may do,
/// versus whether they are actually here.
///
/// Why a subject rather than a user: people share a project with a role far more often than with a
/// list of individuals. Expanding a role into rows would freeze the membership at grant time.
/// </remarks>
[GenerateMapper]
public sealed partial class UniverseMemberConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the row name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the configuration section name.</summary>
    public string SectionName => "UniverseMembers";

    /// <summary>Gets the structural discriminator.</summary>
    public string ServiceType => "UniverseMember";

    /// <summary>Gets the service option type. Always null — this row selects no factory.</summary>
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the owning universe.</summary>
    public Guid UniverseId { get; set; }

    /// <summary>Gets or sets what kind of thing holds this membership: User or Role.</summary>
    /// <remarks>
    /// A role membership is stored as the role, not expanded into a row per current member, so the
    /// project's access follows the role's own membership instead of a snapshot taken at grant time.
    /// </remarks>
    public string SubjectType { get; set; } = string.Empty;

    /// <summary>Gets or sets the user or role that holds this membership.</summary>
    public Guid SubjectId { get; set; }

    /// <summary>Gets or sets the role held: Owner, Steward, Contributor or Consumer.</summary>
    public string MemberRole { get; set; } = string.Empty;

    /// <summary>Gets or sets the membership state: Invited, Active, Suspended or Left.</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>Gets or sets when the invitation was accepted.</summary>
    public DateTimeOffset? JoinedAt { get; set; }

    /// <summary>Gets or sets who issued the invitation.</summary>
    public Guid? InvitedByUserId { get; set; }

    /// <summary>Gets or sets the optional tenant scope.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Gets or sets the optional row-level visibility group.</summary>
    public Guid? VisibilityGroupId { get; set; }

    /// <summary>Gets or sets whether this is the current active version of the row.</summary>
    public bool IsCurrent { get; set; } = true;

    /// <summary>Gets or sets whether the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Gets or sets the original creation date from the source system.</summary>
    public DateTimeOffset? SrcCreateDate { get; set; }

    /// <summary>Gets or sets the timestamp when the row was created.</summary>
    public DateTimeOffset CreateDate { get; set; }

    /// <summary>Gets or sets the database user who created the row.</summary>
    public string CreateBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was created.</summary>
    public string CreateOnBehalfOf { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the row was last modified.</summary>
    public DateTimeOffset ModifyDate { get; set; }

    /// <summary>Gets or sets the database user who last modified the row.</summary>
    public string ModifyBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the application user on whose behalf the row was last modified.</summary>
    public string ModifyOnBehalfOf { get; set; } = string.Empty;
}
