using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema.Ddl.Definitions;

/// <summary>Cascade the operation to dependent rows.</summary>
[TypeOption(typeof(DdlForeignKeyActions), "Cascade")]
[ExcludeFromCodeCoverage]
public sealed class CascadeDdlForeignKeyAction : DdlForeignKeyActionBase
{
    /// <summary>Initializes a new instance of <see cref="CascadeDdlForeignKeyAction"/>.</summary>
    public CascadeDdlForeignKeyAction() : base(2, "Cascade") { }
}
