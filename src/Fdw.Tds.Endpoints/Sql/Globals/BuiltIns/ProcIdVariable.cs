using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@PROCID: the id of the running procedure; none runs here.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "ProcId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ProcIdVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="ProcIdVariable"/> class.</summary>
    public ProcIdVariable()
        : base(name: "ProcId", sqlName: "@@PROCID")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => 0;
}
