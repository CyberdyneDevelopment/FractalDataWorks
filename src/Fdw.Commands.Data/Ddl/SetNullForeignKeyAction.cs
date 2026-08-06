using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Set foreign key to NULL when referenced row is deleted/updated.</summary>
[TypeOption(typeof(ForeignKeyActions), "SetNull")]
[ExcludeFromCodeCoverage]
public sealed class SetNullForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetNullForeignKeyAction"/>.</summary>
    public SetNullForeignKeyAction() : base(2, "SetNull") { }
}
