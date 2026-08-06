namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server-specific typed body for a data container key field participation.
/// Maps to <c>data.MsSqlDataContainerKeyField</c>, joined to <c>data.DataContainerKeyField</c>
/// by the <c>DataContainerKeyFieldRowId</c> column.
/// </summary>
public interface IMsSqlDataContainerKeyField
{
    /// <summary>
    /// Gets the sort direction for this field's participation in the index.
    /// Expected values: "Asc" or "Desc".
    /// </summary>
    string SortDirection { get; }

    /// <summary>
    /// Gets whether this field is an included (non-key) column in a covering index,
    /// rather than a key column.
    /// </summary>
    bool IsIncluded { get; }
}
