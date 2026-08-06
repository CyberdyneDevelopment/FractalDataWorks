using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Path type for SQL Server database paths (Database.Schema.Object format).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathTypes), "DatabasePath", RestrictToCurrentCompilation = true)]
public sealed class DatabasePathType : PathTypeBase
{
    /// <summary>
    /// Singleton instance of DatabasePathType.
    /// </summary>
    public static readonly DatabasePathType Instance = new();

    private DatabasePathType()
        : base(
            id: 1,
            name: "DatabasePath",
            displayName: "Database Path",
            description: "Navigates to SQL Server database objects using Database.Schema.Object format",
            domain: "Sql")
    {
    }
}
