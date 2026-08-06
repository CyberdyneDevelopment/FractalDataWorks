namespace Fdw.Data.Abstractions;

/// <summary>
/// Pre-resolved foreign key mapping for a container field.
/// Stored in <c>IStorageContainer.Metadata["ForeignKeyFields"]</c> as a
/// <c>IReadOnlyList&lt;ForeignKeyResolution&gt;</c> so translators can generate
/// parent-lookup subqueries without access to the full configuration graph.
/// </summary>
/// <param name="FkColumnName">
/// The FK column in the child table (e.g., <c>ConnectionRowId</c>).
/// Resolved from the FK entry's own <c>DataContainerFieldRowId</c> → field name.
/// </param>
/// <param name="ParentSchema">
/// The SQL schema of the parent table (e.g., <c>conn</c>).
/// Resolved via <c>ReferencedFieldRowId</c> → DataContainerField → DataContainer → DataPath.Name.
/// </param>
/// <param name="ParentTableName">
/// The SQL table name of the parent (e.g., <c>Connection</c>).
/// Resolved via <c>ReferencedFieldRowId</c> → DataContainerField.DataContainerId → DataContainer.Name.
/// </param>
/// <param name="LogicalIdParameterName">
/// The child POCO property name used as the subquery parameter (e.g., <c>ConnectionId</c>).
/// Derived from the FK column name by replacing the <c>RowId</c> suffix with <c>Id</c> —
/// a child-side naming convention, not a cross-table reference.
/// The PocoMapper always has this property; only the physical RowId column is absent.
/// </param>
// Why: pure positional record (DTO), auto-generated properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record ForeignKeyResolution(
    string FkColumnName,
    string ParentSchema,
    string ParentTableName,
    string LogicalIdParameterName);
