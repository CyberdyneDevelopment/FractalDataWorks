using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Drops a table.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropTable")]
[ExcludeFromCodeCoverage]
public sealed class DropTableDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropTableDdlCommandType"/>.</summary>
    public DropTableDdlCommandType() : base(4, "DropTable") { }
}
