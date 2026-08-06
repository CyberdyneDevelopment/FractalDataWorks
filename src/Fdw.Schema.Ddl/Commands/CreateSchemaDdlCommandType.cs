using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Creates a new database schema.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateSchema")]
[ExcludeFromCodeCoverage]
public sealed class CreateSchemaDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateSchemaDdlCommandType"/>.</summary>
    public CreateSchemaDdlCommandType() : base(1, "CreateSchema") { }
}
