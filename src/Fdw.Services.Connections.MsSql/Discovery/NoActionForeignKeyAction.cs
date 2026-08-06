using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>No action is taken; constraint violation results in error.</summary>
[TypeOption(typeof(ForeignKeyActions), "NoAction")]
[ExcludeFromCodeCoverage]
public sealed class NoActionForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="NoActionForeignKeyAction"/>.</summary>
    public NoActionForeignKeyAction() : base(0, "NoAction") { }
}
