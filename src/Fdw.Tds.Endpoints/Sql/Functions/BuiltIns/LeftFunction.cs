using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// LEFT(text, n): the first n characters.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Left", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LeftFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="LeftFunction"/> class.</summary>
    public LeftFunction()
        : base(name: "Left", sqlName: "LEFT", fewestArguments: 2, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        var count = call.Whole(1);
        if (text.IsFailure || count.IsFailure)
            return FirstFailure.Of(text, count);

        return text.Value is null || count.Value is not { } n ? SqlFunctionCall.Ok(null) : TextFunctions.Left(call, text.Value, n);
    }
}
