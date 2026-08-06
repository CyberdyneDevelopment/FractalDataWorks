using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>Set foreign key columns to NULL.</summary>
[TypeOption(typeof(ForeignKeyActions), "SetNull")]
[ExcludeFromCodeCoverage]
public sealed class SetNullForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetNullForeignKeyAction"/>.</summary>
    public SetNullForeignKeyAction() : base(2, "SetNull") { }
}
