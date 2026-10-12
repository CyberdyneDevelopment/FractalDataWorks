using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.SchemaObjects.Abstractions;

/// <summary>
/// Schema object type for foreign keys.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SchemaObjectTypes), "ForeignKey", RestrictToCurrentCompilation = true)]
public sealed class ForeignKeyObjectType : SchemaObjectTypeBase
{
    /// <summary>
    /// Material Symbols icon name for foreign keys.
    /// </summary>
    public const string ForeignKeyIcon = "link";

    /// <summary>
    /// Initializes a new instance of the <see cref="ForeignKeyObjectType"/> class.
    /// </summary>
    public ForeignKeyObjectType()
        : base(
            name: "ForeignKey",
            icon: ForeignKeyIcon,
            color: "Tertiary",
            cssClass: "schema-foreignkey",
            canHaveChildren: false,
            sortOrder: 8)
    {
    }
}
