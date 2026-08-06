using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>DROP TABLE operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropTable")]
[ExcludeFromCodeCoverage]
public sealed class DropTableDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropTableDdlCommandType"/>.</summary>
    public DropTableDdlCommandType() : base(3, "DropTable") { }
}
