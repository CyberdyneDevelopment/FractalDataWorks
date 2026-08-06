using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>Set referencing column to NULL.</summary>
[TypeOption(typeof(ForeignKeyActions), "SetNull")]
[ExcludeFromCodeCoverage]
public sealed class SetNullForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="SetNullForeignKeyAction"/>.</summary>
    public SetNullForeignKeyAction() : base(3, "SetNull") { }
}
