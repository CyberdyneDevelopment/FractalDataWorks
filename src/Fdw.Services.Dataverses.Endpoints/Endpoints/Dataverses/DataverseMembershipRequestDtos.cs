namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Projects a stored membership request onto its DTO.</summary>
/// <remarks>Shared by every endpoint in this file group that returns one, so the shape is defined once.</remarks>
internal static class DataverseMembershipRequestDtos
{
    /// <summary>Projects one row.</summary>
    public static DataverseMembershipRequestDto From(DataverseMembershipRequestConfiguration request) => new()
    {
        Id = request.Id,
        RequestedByUserId = request.RequestedByUserId,
        SubjectType = request.SubjectType,
        SubjectId = request.SubjectId,
        RequestedRole = request.RequestedRole,
        Justification = request.Justification,
        Status = request.Status,
        ReviewedByUserId = request.ReviewedByUserId,
        ReviewedAt = request.ReviewedAt,
        ReviewNotes = request.ReviewNotes,
    };
}
