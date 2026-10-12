using System;
using System.Collections.Generic;
using Fdw.Web.Clients.Abstractions.Contracts;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Response for data preview.
/// </summary>
public sealed class DataPreviewResponsePayload : IDataPreviewResponse
{
    /// <summary>Gets or sets the list of column definitions.</summary>
    public IReadOnlyList<ColumnSchemaPayload> Columns { get; set; } = Array.Empty<ColumnSchemaPayload>();
    /// <summary>Gets or sets the preview data rows.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = Array.Empty<IReadOnlyDictionary<string, object?>>();
    /// <summary>Gets or sets the estimated total row count.</summary>
    public long? TotalRowCount { get; set; }
    /// <summary>Gets or sets a value indicating whether more rows are available.</summary>
    public bool HasMoreRows { get; set; }

    /// <inheritdoc />
    IReadOnlyList<IColumnSchema> IDataPreviewResponse.Columns => Columns;
}
