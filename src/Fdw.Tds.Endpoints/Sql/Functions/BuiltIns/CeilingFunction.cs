using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// CEILING(number): the smallest whole number not below it, of the number's own type.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Ceiling", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CeilingFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="CeilingFunction"/> class.</summary>
    public CeilingFunction()
        : base(name: "Ceiling", sqlName: "CEILING", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var value = call.Value(0);
        if (value.IsFailure || value.Value is null)
            return value;

        return NumberFunctions.Map(call, value.Value, static n => n, decimal.Ceiling, Math.Ceiling);
    }
}
