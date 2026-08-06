using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>No action is taken (default behavior).</summary>
[TypeOption(typeof(DdlForeignKeyActions), "NoAction")]
[ExcludeFromCodeCoverage]
public sealed class NoActionDdlForeignKeyAction : DdlForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="NoActionDdlForeignKeyAction"/>.</summary>
    public NoActionDdlForeignKeyAction() : base(1, "NoAction") { }
}
