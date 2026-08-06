using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>Set foreign key columns to their default values.</summary>
[TypeOption(typeof(ForeignKeyActions), "SetDefault")]
[ExcludeFromCodeCoverage]
public sealed class SetDefaultForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetDefaultForeignKeyAction"/>.</summary>
    public SetDefaultForeignKeyAction() : base(3, "SetDefault") { }
}
