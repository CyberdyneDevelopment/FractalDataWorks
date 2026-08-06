using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>CREATE TABLE operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateTable")]
[ExcludeFromCodeCoverage]
public sealed class CreateTableDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateTableDdlCommandType"/>.</summary>
    public CreateTableDdlCommandType() : base(1, "CreateTable") { }
}
