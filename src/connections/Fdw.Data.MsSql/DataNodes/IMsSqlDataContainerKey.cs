namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server-specific typed body for a data container key.
/// Maps to <c>data.MsSqlDataContainerKey</c>, joined to <c>data.DataContainerKey</c>
/// by the <c>DataContainerKeyRowId</c> column.
/// </summary>
public interface IMsSqlDataContainerKey
{
    /// <summary>Gets the constraint name as known to SQL Server (e.g., "PK_Connection").</summary>
    string? ConstraintName { get; }

    /// <summary>Gets whether the index behind this key is clustered.</summary>
    bool IsClustered { get; }

    /// <summary>Gets whether the index enforces uniqueness.</summary>
    bool IsUnique { get; }

    /// <summary>Gets whether index pages are padded.</summary>
    bool IsPadded { get; }

    /// <summary>Gets the fill factor percentage, or <see langword="null"/> when using the server default.</summary>
    int? FillFactor { get; }
}
