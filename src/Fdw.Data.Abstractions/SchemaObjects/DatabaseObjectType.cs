using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for databases.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "Database", RestrictToCurrentCompilation = true)]
public sealed class DatabaseObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// Material Symbols icon name for databases.
    /// </summary>
    public const string DatabaseIcon = "database";

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseObjectType"/> class.
    /// </summary>
    public DatabaseObjectType()
        : base(
            name: "Database",
            icon: DatabaseIcon,
            color: "Primary",
            cssClass: "schema-database",
            canHaveChildren: true,
            sortOrder: 1)
    {
    }
}
