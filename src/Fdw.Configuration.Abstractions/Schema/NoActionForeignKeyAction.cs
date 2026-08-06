using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>No action on referenced row change.</summary>
[TypeOption(typeof(ForeignKeyActions), "NoAction")]
[ExcludeFromCodeCoverage]
public sealed class NoActionForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="NoActionForeignKeyAction"/>.</summary>
    public NoActionForeignKeyAction() : base(1, "NoAction") { }
}
