using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>Set referencing column to default value.</summary>
[TypeOption(typeof(ForeignKeyActions), "SetDefault")]
[ExcludeFromCodeCoverage]
public sealed class SetDefaultForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetDefaultForeignKeyAction"/>.</summary>
    public SetDefaultForeignKeyAction() : base(4, "SetDefault") { }
}
