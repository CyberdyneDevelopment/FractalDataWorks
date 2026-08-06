using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Data.Ddl;

/// <summary>No action (default). Prevents delete/update if references exist.</summary>
[TypeOption(typeof(ForeignKeyActions), "NoAction")]
[ExcludeFromCodeCoverage]
public sealed class NoActionForeignKeyAction : ForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="NoActionForeignKeyAction"/>.</summary>
    public NoActionForeignKeyAction() : base(0, "NoAction") { }
}
