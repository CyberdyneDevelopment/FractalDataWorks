using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>ALTER TABLE operation (add/drop/modify columns).</summary>
[TypeOption(typeof(DdlCommandTypes), "AlterTable")]
[ExcludeFromCodeCoverage]
public sealed class AlterTableDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="AlterTableDdlCommandType"/>.</summary>
    public AlterTableDdlCommandType() : base(2, "AlterTable") { }
}
