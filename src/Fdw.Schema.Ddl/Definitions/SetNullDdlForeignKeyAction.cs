using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>Set the foreign key column to NULL.</summary>
[TypeOption(typeof(DdlForeignKeyActions), "SetNull")]
[ExcludeFromCodeCoverage]
public sealed class SetNullDdlForeignKeyAction : DdlForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetNullDdlForeignKeyAction"/>.</summary>
    public SetNullDdlForeignKeyAction() : base(3, "SetNull") { }
}
