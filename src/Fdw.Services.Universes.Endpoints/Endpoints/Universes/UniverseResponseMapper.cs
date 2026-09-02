using System.Linq;

namespace Fdw.Services.Universes.Endpoints;

/// <summary>
/// Projects universe configurations onto their wire shapes.
/// </summary>
/// <remarks>
/// One place, because five endpoints return the same two shapes and a per-endpoint projection is
/// how two of them quietly start disagreeing about what a universe looks like.
/// </remarks>
internal static class UniverseResponseMapper
{
    /// <summary>Projects a configuration onto its list shape.</summary>
    /// <param name="config">The universe configuration.</param>
    internal static UniverseSummaryResponse ToSummary(UniverseConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Description = config.Description,
        Status = config.Status,
        Visibility = config.Visibility,
        JoinPolicy = config.JoinPolicy,
        OwnerUserId = config.OwnerUserId,
        MemberCount = config.Members.Count,
        ResourceCount = config.Resources.Count,
        CreatedAt = config.CreateDate,
        ModifiedAt = config.ModifyDate,
    };

    /// <summary>Projects a configuration onto its detail shape, children included.</summary>
    /// <param name="config">The universe configuration.</param>
    internal static UniverseDetailResponse ToDetail(UniverseConfiguration config) => new()
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
        Members = config.Members.Select(m => new UniverseMemberDto
        {
            Id = m.Id,
            SubjectType = m.SubjectType,
            SubjectId = m.SubjectId,
            MemberRole = m.MemberRole,
            State = m.State,
            JoinedAt = m.JoinedAt,
        }).ToList(),
        Resources = config.Resources.Select(r => new UniverseResourceDto
        {
            Id = r.Id,
            ResourceType = r.ResourceType,
            ResourceId = r.ResourceId,
            Relationship = r.Relationship,
        }).ToList(),
        Relationships = config.Relationships.Select(r => new UniverseRelationshipDto
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
