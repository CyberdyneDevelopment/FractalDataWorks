using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Projects data views onto their wire shapes.</summary>
/// <remarks>One place, because every endpoint in this group returns the same shapes.</remarks>
internal static class DataViewResponseMapper
{
    /// <summary>Projects a data view onto its whole shape.</summary>
    /// <param name="config">The data view.</param>
    /// <param name="parentName">The name of the view it is a view of, or null when it is a view of a data set.</param>
    internal static DataViewResponse ToResponse(IDataViewDataSetVariationConfiguration config, string? parentName) => new()
    {
        Id = config.Id,
        Name = config.Name,
        Kind = config.Implementation,
        ParentName = parentName,
        DisplayName = config.DisplayName,
        Description = config.Description,
        SubjectDataSetId = config.BaseDataSetId,
        Encoding = config.Encoding,
        Filters = config.Filters,
        ChartType = config.ChartType,
        OwnerUserId = config.OwnerUserId,
        CreatedAt = config.CreatedAt,
    };

    /// <summary>Projects a data view's domain row onto its list shape: everything the domain table holds.</summary>
    /// <param name="row">The data view, as the variation domain table holds it.</param>
    /// <param name="parentName">The name of the view it is a view of, or null when it is a view of a data set.</param>
    internal static DataViewSummaryResponse ToSummary(DataSetVariationDomainConfiguration row, string? parentName) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Kind = row.Implementation,
        ParentName = parentName,
        DisplayName = row.DisplayName,
        Description = row.Description,
        SubjectDataSetId = row.BaseDataSetId,
        OwnerUserId = row.OwnerUserId,
        CreatedAt = row.CreatedAt,
    };
}
