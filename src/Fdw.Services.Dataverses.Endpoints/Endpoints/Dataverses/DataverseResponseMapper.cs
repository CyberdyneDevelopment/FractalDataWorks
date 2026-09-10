using System.Linq;

namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>
/// Projects dataverse configurations onto their wire shapes.
/// </summary>
/// <remarks>
/// One place, because five endpoints return the same two shapes and a per-endpoint projection is
/// how two of them quietly start disagreeing about what a dataverse looks like.
/// </remarks>
internal static class DataverseResponseMapper
{
    /// <summary>Projects a configuration onto its list shape.</summary>
    /// <param name="config">The dataverse configuration.</param>
    internal static DataverseSummaryResponse ToSummary(DataverseImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Description = config.Description,
        Purpose = config.Purpose,
        Status = config.Status,
        Visibility = config.Visibility,
        JoinPolicy = config.JoinPolicy,
        OwnerUserId = config.OwnerUserId,
        CreatedAt = config.CreateDate,
    };
    /// <summary>Projects a configuration onto its detail shape, children included.</summary>
    /// <param name="config">The dataverse configuration.</param>
    internal static DataverseDetailResponse ToDetail(DataverseImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Description = config.Description,
        Purpose = config.Purpose,
        Status = config.Status,
        Visibility = config.Visibility,
        JoinPolicy = config.JoinPolicy,
        OwnerUserId = config.OwnerUserId,
        StandInSeed = config.StandInSeed,
        CreatedAt = config.CreateDate,
        ModifiedAt = config.ModifyDate,
        Members = config.Members.Select(m => new DataverseMemberDto
        {
            Id = m.Id,
            SubjectType = m.SubjectType,
            SubjectId = m.SubjectId,
            MemberRole = m.MemberRole,
            State = m.State,
            JoinedAt = m.JoinedAt,
        }).ToList(),
        Resources = config.Resources.Select(r => new DataverseResourceDto
        {
            Id = r.Id,
            ResourceType = r.ResourceType,
            ResourceId = r.ResourceId,
            Relationship = r.Relationship,
        }).ToList(),
        Relationships = config.Relationships.Select(r => new DataverseRelationshipDto
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
