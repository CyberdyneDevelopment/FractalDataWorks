using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Restrict delete/update (similar to NoAction, but checked immediately).</summary>
[TypeOption(typeof(ForeignKeyActions), "Restrict")]
[ExcludeFromCodeCoverage]
public sealed class RestrictForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="RestrictForeignKeyAction"/>.</summary>
    public RestrictForeignKeyAction() : base(4, "Restrict") { }
}
