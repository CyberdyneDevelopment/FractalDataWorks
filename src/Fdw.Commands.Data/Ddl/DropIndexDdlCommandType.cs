using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>DROP INDEX operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropIndex")]
[ExcludeFromCodeCoverage]
public sealed class DropIndexDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropIndexDdlCommandType"/>.</summary>
    public DropIndexDdlCommandType() : base(5, "DropIndex") { }
}
