using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Creates a new index.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateIndex")]
[ExcludeFromCodeCoverage]
public sealed class CreateIndexDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateIndexDdlCommandType"/>.</summary>
    public CreateIndexDdlCommandType() : base(5, "CreateIndex") { }
}
