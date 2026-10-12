using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@MAX_PRECISION: the largest decimal precision.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "MaxPrecision", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MaxPrecisionVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="MaxPrecisionVariable"/> class.</summary>
    public MaxPrecisionVariable()
        : base(name: "MaxPrecision", sqlName: "@@MAX_PRECISION")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => (byte)38;
}
