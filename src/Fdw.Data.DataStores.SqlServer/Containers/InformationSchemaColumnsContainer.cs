using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataStores.SqlServer.Models;

namespace Fdw.Data.DataStores.SqlServer.Containers;

/// <summary>
/// Container definition for INFORMATION_SCHEMA.COLUMNS with extended metadata.
/// Used by SqlServerSchemaImporter to query column metadata with primary key and identity information.
/// </summary>
/// <remarks>
/// This class serves as documentation for the container structure.
/// Maps to a custom query joining INFORMATION_SCHEMA.COLUMNS with KEY_COLUMN_USAGE.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public static class InformationSchemaColumnsContainer
{
    /// <summary>
    /// Gets the container ID.
    /// </summary>
    public static string Id => "INFORMATION_SCHEMA.COLUMNS_EXTENDED";

    /// <summary>
    /// Gets the container name.
    /// </summary>
    public static string Name => "COLUMNS";

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public static string Schema => "INFORMATION_SCHEMA";

    /// <summary>
    /// Gets the full path.
    /// </summary>
    public static string FullPath => "[INFORMATION_SCHEMA].[COLUMNS]";
}
