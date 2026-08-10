using System.Collections.Generic;
using Fdw.Services.Data.Clients.Models;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for previewing data from a named DataSet with pagination and filter support.
/// The DataSet name is bound from the route: <c>POST /datasets/{name}/preview</c>.
/// </summary>
public class DataSetPreviewRequest
{
    /// <summary>Gets or sets the DataSet name (bound from route).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of rows to return (capped server-side).</summary>
    public int MaxRows { get; set; } = 100;

    /// <summary>Gets or sets the 1-based page number for paginated results.</summary>
    public int Page { get; set; } = 1;

    /// <summary>Gets or sets the number of rows per page.</summary>
    public int PageSize { get; set; } = 50;

    /// <summary>Gets or sets ad-hoc filter conditions to apply to the preview query.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];
}
