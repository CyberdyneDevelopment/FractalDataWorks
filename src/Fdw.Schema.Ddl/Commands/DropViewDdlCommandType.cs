using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Drops a view.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropView")]
[ExcludeFromCodeCoverage]
public sealed class DropViewDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropViewDdlCommandType"/>.</summary>
    public DropViewDdlCommandType() : base(8, "DropView") { }
}
