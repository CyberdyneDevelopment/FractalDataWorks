using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Drops a schema.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropSchema")]
[ExcludeFromCodeCoverage]
public sealed class DropSchemaDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropSchemaDdlCommandType"/>.</summary>
    public DropSchemaDdlCommandType() : base(9, "DropSchema") { }
}
