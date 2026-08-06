using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Set foreign key to default value when referenced row is deleted/updated.</summary>
[TypeOption(typeof(ForeignKeyActions), "SetDefault")]
[ExcludeFromCodeCoverage]
public sealed class SetDefaultForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetDefaultForeignKeyAction"/>.</summary>
    public SetDefaultForeignKeyAction() : base(3, "SetDefault") { }
}
