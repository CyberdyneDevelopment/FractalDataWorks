using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Creates a new table.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateTable")]
[ExcludeFromCodeCoverage]
public sealed class CreateTableDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateTableDdlCommandType"/>.</summary>
    public CreateTableDdlCommandType() : base(2, "CreateTable") { }
}
