using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>CREATE INDEX operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateIndex")]
[ExcludeFromCodeCoverage]
public sealed class CreateIndexDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateIndexDdlCommandType"/>.</summary>
    public CreateIndexDdlCommandType() : base(4, "CreateIndex") { }
}
