using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataStores.SqlServer.Models;

namespace Fdw.Data.DataStores.SqlServer.Containers;

/// <summary>
/// Container definition for INFORMATION_SCHEMA.TABLES.
/// Used by SqlServerSchemaImporter to query table metadata.
/// </summary>
/// <remarks>
/// This class serves as documentation for the container structure.
/// Maps to the INFORMATION_SCHEMA.TABLES system view in SQL Server.
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
public static class InformationSchemaTablesContainer
{
    /// <summary>
    /// Gets the container ID.
    /// </summary>
    public static string Id => "INFORMATION_SCHEMA.TABLES";

    /// <summary>
    /// Gets the container name.
    /// </summary>
    public static string Name => "TABLES";

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public static string Schema => "INFORMATION_SCHEMA";

    /// <summary>
    /// Gets the full path.
    /// </summary>
    public static string FullPath => "[INFORMATION_SCHEMA].[TABLES]";
}
