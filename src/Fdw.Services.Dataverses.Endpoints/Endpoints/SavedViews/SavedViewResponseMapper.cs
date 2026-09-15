namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Projects saved view configurations onto their wire shape.</summary>
/// <remarks>One place, because both endpoints in this file group return the same shape.</remarks>
internal static class SavedViewResponseMapper
{
    /// <summary>Projects a configuration onto its response shape.</summary>
    /// <param name="config">The saved view configuration.</param>
    internal static SavedViewResponse ToResponse(ISavedViewImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Description = config.Description,
        SubjectDataSetId = config.SubjectDataSetId,
        Encoding = config.Encoding,
        Filters = config.Filters,
        ChartType = config.ChartType,
        OwnerUserId = config.OwnerUserId,
        CreatedAt = config.CreateDate,
    };
}
