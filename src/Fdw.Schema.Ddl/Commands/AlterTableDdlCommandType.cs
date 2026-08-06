using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Alters an existing table.</summary>
[TypeOption(typeof(DdlCommandTypes), "AlterTable")]
[ExcludeFromCodeCoverage]
public sealed class AlterTableDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AlterTableDdlCommandType"/>.</summary>
    public AlterTableDdlCommandType() : base(3, "AlterTable") { }
}
