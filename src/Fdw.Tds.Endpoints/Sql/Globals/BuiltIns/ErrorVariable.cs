using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@ERROR: the error number of the last statement; a statement that failed ends the batch, so a statement reading it follows one that succeeded.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "Error", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ErrorVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="ErrorVariable"/> class.</summary>
    public ErrorVariable()
        : base(name: "Error", sqlName: "@@ERROR")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => 0;
}
