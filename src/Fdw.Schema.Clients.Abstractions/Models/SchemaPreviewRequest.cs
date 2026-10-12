using System.Collections.Generic;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Request for schema data preview via DataStore container endpoint. Uses column-based
/// <see cref="PreviewFilterCondition"/> filters. For paginated DataSet preview use
/// <c>Fdw.Services.Data.Clients.Models.DataPreviewRequestPayload</c> instead.
/// </summary>
public sealed class SchemaPreviewRequest : IDataPreviewRequest
{
    /// <summary>Gets or sets the optional DataSet name.</summary>
    public string? DataSetName { get; set; }
    /// <summary>Gets or sets the optional DataStore name.</summary>
    public string? DataStoreName { get; set; }
    /// <summary>Gets or sets the optional path name within the DataStore.</summary>
    public string? PathName { get; set; }
    /// <summary>Gets or sets the optional container name within the path.</summary>
    public string? ContainerName { get; set; }
    /// <summary>Gets or sets the maximum number of rows to return.</summary>
    public int MaxRows { get; set; } = 100;
    /// <summary>Gets or sets optional filter conditions for the preview query.</summary>
    public IList<PreviewFilterCondition>? Filters { get; set; }
}
