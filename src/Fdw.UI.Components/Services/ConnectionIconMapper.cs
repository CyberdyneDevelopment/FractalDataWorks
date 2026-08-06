namespace Fdw.UI.Components.Services;

using System;

/// <summary>
/// Maps connection type names to icon categories and display metadata.
/// Framework-agnostic — returns semantic identifiers that consumers map to their icon library.
/// </summary>
public static class ConnectionIconMapper
{
    /// <summary>
    /// Gets icon metadata for a connection type.
    /// </summary>
    /// <param name="connectionType">The connection type name (e.g., "MsSql", "PostgreSql").</param>
    /// <returns>A <see cref="ConnectionIcon"/> with category, icon key, and label.</returns>
    public static ConnectionIcon FromType(string? connectionType)
    {
        if (string.IsNullOrEmpty(connectionType))
        {
            return new ConnectionIcon("Unknown", "database", "unknown");
        }

        if (connectionType.Contains("MsSql", StringComparison.OrdinalIgnoreCase) ||
            connectionType.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("SQL Server", "database", "mssql");
        }

        if (connectionType.Contains("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("PostgreSQL", "database", "postgresql");
        }

        if (connectionType.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("MySQL", "database", "mysql");
        }

        if (connectionType.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("SQLite", "database", "sqlite");
        }

        if (connectionType.Contains("Http", StringComparison.OrdinalIgnoreCase) ||
            connectionType.Contains("Rest", StringComparison.OrdinalIgnoreCase) ||
            connectionType.Contains("Api", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("HTTP/REST", "cloud", "http");
        }

        if (connectionType.Contains("File", StringComparison.OrdinalIgnoreCase) ||
            connectionType.Contains("Csv", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("File", "file", "file");
        }

        if (connectionType.Contains("Blob", StringComparison.OrdinalIgnoreCase) ||
            connectionType.Contains("Storage", StringComparison.OrdinalIgnoreCase) ||
            connectionType.Contains("S3", StringComparison.OrdinalIgnoreCase))
        {
            return new ConnectionIcon("Storage", "cloud", "storage");
        }

        return new ConnectionIcon(connectionType, "database", connectionType.ToLowerInvariant());
    }
}
