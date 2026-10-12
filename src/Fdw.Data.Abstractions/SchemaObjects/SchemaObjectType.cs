using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for database schemas.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "Schema", RestrictToCurrentCompilation = true)]
public sealed class SchemaObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// Material Symbols icon name for schemas.
    /// </summary>
    public const string SchemaIcon = "folder";

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaObjectType"/> class.
    /// </summary>
    public SchemaObjectType()
        : base(
            name: "Schema",
            icon: SchemaIcon,
            color: "Default",
            cssClass: "schema-schema",
            canHaveChildren: true,
            sortOrder: 2)
    {
    }
}
