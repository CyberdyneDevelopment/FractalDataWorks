using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>CREATE VIEW operation.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateView")]
[ExcludeFromCodeCoverage]
public sealed class CreateViewDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateViewDdlCommandType"/>.</summary>
    public CreateViewDdlCommandType() : base(6, "CreateView") { }
}
