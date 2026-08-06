using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>Set the foreign key column to its default value.</summary>
[TypeOption(typeof(DdlForeignKeyActions), "SetDefault")]
[ExcludeFromCodeCoverage]
public sealed class SetDefaultDdlForeignKeyAction : DdlForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetDefaultDdlForeignKeyAction"/>.</summary>
    public SetDefaultDdlForeignKeyAction() : base(4, "SetDefault") { }
}
