using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>DROP SCHEMA operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "DropSchema")]
[ExcludeFromCodeCoverage]
public sealed class DropSchemaDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DropSchemaDdlCommandType"/>.</summary>
    public DropSchemaDdlCommandType() : base(9, "DropSchema") { }
}
