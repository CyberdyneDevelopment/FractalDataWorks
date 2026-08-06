using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>CREATE SCHEMA operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateSchema")]
[ExcludeFromCodeCoverage]
public sealed class CreateSchemaDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateSchemaDdlCommandType"/>.</summary>
    public CreateSchemaDdlCommandType() : base(8, "CreateSchema") { }
}
