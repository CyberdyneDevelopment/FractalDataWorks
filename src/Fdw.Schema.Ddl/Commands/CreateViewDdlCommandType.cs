using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Creates a new view.</summary>
[TypeOption(typeof(DdlCommandTypes), "CreateView")]
[ExcludeFromCodeCoverage]
public sealed class CreateViewDdlCommandType : DdlCommandTypeBase
{
    /// <summary>Initializes a new instance of <see cref="CreateViewDdlCommandType"/>.</summary>
    public CreateViewDdlCommandType() : base(7, "CreateView") { }
}
