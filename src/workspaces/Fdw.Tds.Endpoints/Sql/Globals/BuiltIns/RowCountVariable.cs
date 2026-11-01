using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@ROWCOUNT: the rows the last statement returned.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "RowCount", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RowCountVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="RowCountVariable"/> class.</summary>
    public RowCountVariable()
        : base(name: "RowCount", sqlName: "@@ROWCOUNT")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => (int)System.Math.Min(context.Session.LastRowCount, int.MaxValue);
}
