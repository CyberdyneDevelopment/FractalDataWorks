namespace Fdw.Web.Clients.Abstractions.Contracts;

using System.Collections.Generic;

/// <summary>
/// Abstraction for data preview responses used across Schema and Data domains.
/// </summary>
public interface IDataPreviewResponse
{
    /// <summary>Gets the list of column definitions.</summary>
    IReadOnlyList<IColumnSchema> Columns { get; }
    /// <summary>Gets the preview data rows.</summary>
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; }
    /// <summary>Gets the estimated total row count.</summary>
    long? TotalRowCount { get; }
    /// <summary>Gets a value indicating whether more rows are available.</summary>
    bool HasMoreRows { get; }
}
