using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>DROP VIEW operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropView")]
[ExcludeFromCodeCoverage]
public sealed class DropViewDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropViewDdlCommandType"/>.</summary>
    public DropViewDdlCommandType() : base(7, "DropView") { }
}
