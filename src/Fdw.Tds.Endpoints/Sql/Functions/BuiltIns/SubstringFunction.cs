using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SUBSTRING(text, start, length): the characters from a 1-based start; a start before 1 still counts toward the length, as SQL Server counts it.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Substring", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SubstringFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SubstringFunction"/> class.</summary>
    public SubstringFunction()
        : base(name: "Substring", sqlName: "SUBSTRING", fewestArguments: 3, mostArguments: 3)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        var start = call.Whole(1);
        var length = call.Whole(2);
        if (text.IsFailure || start.IsFailure || length.IsFailure)
            return FirstFailure.Of(text, start, length);

        if (text.Value is null || start.Value is not { } from || length.Value is not { } count)
            return SqlFunctionCall.Ok(null);

        return TextFunctions.Substring(call, text.Value, from, count);
    }
}
