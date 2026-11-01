using System.Collections.Generic;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Request for data preview.
/// </summary>
public sealed class DataPreviewRequestPayload : IDataPreviewRequest
{
    /// <summary>Gets or sets the optional DataSet name.</summary>
    public string? DataSetName { get; set; }
    /// <summary>Gets or sets the optional DataStore name.</summary>
    public string? DataStoreName { get; set; }
    /// <summary>Gets or sets the optional path name within the DataStore.</summary>
    public string? PathName { get; set; }
    /// <summary>Gets or sets the optional container name within the path.</summary>
    public string? ContainerName { get; set; }
    /// <summary>Gets or sets the maximum number of rows to return. Use <see cref="PageSize"/> for paginated previews.</summary>
    public int MaxRows { get; set; } = 100;
    /// <summary>Gets or sets the 1-based page number for paginated preview.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Gets or sets the number of rows per page for paginated preview.</summary>
    public int PageSize { get; set; } = 50;
    /// <summary>Gets or sets filter conditions to apply to the preview query.</summary>
    public IList<DataSetFilterConditionPayload> Filters { get; set; } = [];
}
