using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@TRANCOUNT: the open transaction count.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "TranCount", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TranCountVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="TranCountVariable"/> class.</summary>
    public TranCountVariable()
        : base(name: "TranCount", sqlName: "@@TRANCOUNT")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Session.TransactionCount;
}
