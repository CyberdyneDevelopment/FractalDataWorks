using System.Linq;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>
/// Projects workspace configurations onto their wire shapes.
/// </summary>
/// <remarks>
/// One place, because five endpoints return the same two shapes and a per-endpoint projection is
/// how two of them quietly start disagreeing about what a workspace looks like.
/// </remarks>
internal static class WorkspaceResponseMapper
{
    // None is a real answer here: no organization names this workspace as its default.
    private static WorkspaceOrganizationReference? Organization(WorkspaceOwnership ownership)
        => ownership.DefaultFor is { } organization
            ? new WorkspaceOrganizationReference { Id = organization.Id, Name = organization.Name }
            : null;

    /// <summary>Projects a configuration onto its list shape.</summary>
    /// <param name="config">The workspace configuration.</param>
    /// <param name="ownership">The tenant that owns it and the organization that names it as its default.</param>
    internal static WorkspaceSummaryResponse ToSummary(IWorkspaceImplementationConfiguration config, WorkspaceOwnership ownership) => new()
    {
        Id = config.Id,
        TenantId = ownership.TenantId,
        DefaultForOrganization = Organization(ownership),
        Name = config.Name,
        DisplayName = config.DisplayName,
        Description = config.Description,
        ClassificationId = config.ClassificationId,
        Purpose = config.Purpose,
        Status = config.Status,
        Visibility = config.Visibility,
        JoinPolicy = config.JoinPolicy,
        AutoApproveRole = config.AutoApproveRole,
        OwnerUserId = config.OwnerUserId,
        CreatedAt = config.CreateDate,
    };
    /// <summary>Projects a configuration onto its detail shape, children included.</summary>
    /// <param name="config">The workspace configuration.</param>
    /// <param name="ownership">The tenant that owns it and the organization that names it as its default.</param>
    internal static WorkspaceDetailResponse ToDetail(IWorkspaceImplementationConfiguration config, WorkspaceOwnership ownership) => new()
    {
        Id = config.Id,
        TenantId = ownership.TenantId,
        DefaultForOrganization = Organization(ownership),
        Name = config.Name,
        DisplayName = config.DisplayName,
        Description = config.Description,
        ClassificationId = config.ClassificationId,
        Purpose = config.Purpose,
        Status = config.Status,
        Visibility = config.Visibility,
        JoinPolicy = config.JoinPolicy,
        AutoApproveRole = config.AutoApproveRole,
        OwnerUserId = config.OwnerUserId,
        StandInSeed = config.StandInSeed,
        CreatedAt = config.CreateDate,
        ModifiedAt = config.ModifyDate,
        Members = config.Members.Select(m => new WorkspaceMemberDto
        {
            Id = m.Id,
            SubjectType = m.SubjectType,
            SubjectId = m.SubjectId,
            MemberRole = m.MemberRole,
            State = m.State,
            JoinedAt = m.JoinedAt,
        }).ToList(),
        Resources = config.Resources.Select(r => new WorkspaceResourceDto
        {
            Id = r.Id,
            ResourceType = r.ResourceType,
            ResourceId = r.ResourceId,
            Relationship = r.Relationship,
        }).ToList(),
        Relationships = config.Relationships.Select(r => new WorkspaceRelationshipDto
        {
            Id = r.Id,
            LeftDataSetId = r.LeftDataSetId,
            LeftFieldId = r.LeftFieldId,
            RightDataSetId = r.RightDataSetId,
            RightFieldId = r.RightFieldId,
            Cardinality = r.Cardinality,
        }).ToList(),
    };
}
