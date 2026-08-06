using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>Cascade the operation to related rows.</summary>
[TypeOption(typeof(ForeignKeyActions), "Cascade")]
[ExcludeFromCodeCoverage]
public sealed class CascadeForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="CascadeForeignKeyAction"/>.</summary>
    public CascadeForeignKeyAction() : base(1, "Cascade") { }
}
