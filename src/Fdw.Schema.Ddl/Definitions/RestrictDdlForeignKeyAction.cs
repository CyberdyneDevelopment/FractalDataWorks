using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>Restrict the operation (similar to NoAction but checked immediately).</summary>
[TypeOption(typeof(DdlForeignKeyActions), "Restrict")]
[ExcludeFromCodeCoverage]
public sealed class RestrictDdlForeignKeyAction : DdlForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="RestrictDdlForeignKeyAction"/>.</summary>
    public RestrictDdlForeignKeyAction() : base(5, "Restrict") { }
}
