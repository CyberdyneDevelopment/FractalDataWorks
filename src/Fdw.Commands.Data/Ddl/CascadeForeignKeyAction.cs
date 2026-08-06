using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>Cascade the delete/update to referencing rows.</summary>
[TypeOption(typeof(ForeignKeyActions), "Cascade")]
[ExcludeFromCodeCoverage]
public sealed class CascadeForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="CascadeForeignKeyAction"/>.</summary>
    public CascadeForeignKeyAction() : base(1, "Cascade") { }
}
