using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// ABS(number): the number without its sign, of the number's own type.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Abs", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AbsFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="AbsFunction"/> class.</summary>
    public AbsFunction()
        : base(name: "Abs", sqlName: "ABS", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var value = call.Value(0);
        if (value.IsFailure || value.Value is null)
            return value;

        return NumberFunctions.Map(call, value.Value, Math.Abs, Math.Abs, Math.Abs);
    }
}
