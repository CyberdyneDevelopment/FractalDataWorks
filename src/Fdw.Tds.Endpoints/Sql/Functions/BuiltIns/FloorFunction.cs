using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// FLOOR(number): the largest whole number not above it, of the number's own type.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Floor", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FloorFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="FloorFunction"/> class.</summary>
    public FloorFunction()
        : base(name: "Floor", sqlName: "FLOOR", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var value = call.Value(0);
        if (value.IsFailure || value.Value is null)
            return value;

        return NumberFunctions.Map(call, value.Value, static n => n, decimal.Floor, Math.Floor);
    }
}
