using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// ROUND(number, digits[, truncate]): the number rounded half away from zero to the digits, or cut there when truncate is not 0.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Round", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RoundFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="RoundFunction"/> class.</summary>
    public RoundFunction()
        : base(name: "Round", sqlName: "ROUND", fewestArguments: 2, mostArguments: 3)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var value = call.Value(0);
        var digits = call.Whole(1);
        var truncate = call.Arguments.Count > 2 ? call.Whole(2) : GenericResult<long?>.Success(0);
        if (value.IsFailure || digits.IsFailure || truncate.IsFailure)
            return FirstFailure.Of(value, digits, truncate);

        if (value.Value is null || digits.Value is not { } places)
            return SqlFunctionCall.Ok(null);

        return NumberFunctions.Round(call, value.Value, (int)places, truncate.Value is { } cut && cut != 0);
    }
}
