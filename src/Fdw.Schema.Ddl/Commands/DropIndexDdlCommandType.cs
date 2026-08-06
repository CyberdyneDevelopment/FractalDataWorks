using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Drops an index.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropIndex")]
[ExcludeFromCodeCoverage]
public sealed class DropIndexDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropIndexDdlCommandType"/>.</summary>
    public DropIndexDdlCommandType() : base(6, "DropIndex") { }
}
