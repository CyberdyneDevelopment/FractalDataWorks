using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Path type for SQL Server database paths (Database.Schema.Object format).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PathTypes), "DatabasePath")]
public sealed class DatabasePathType : PathTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabasePathType"/> class.
    /// </summary>
    public DatabasePathType()
        : base(
            name: "DatabasePath",
            displayName: "Database Path",
            description: "Navigates to SQL Server database objects using Database.Schema.Object format",
            domain: "Sql")
    {
    }
}
