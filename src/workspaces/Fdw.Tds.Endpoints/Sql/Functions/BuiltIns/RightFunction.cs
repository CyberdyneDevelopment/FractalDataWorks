using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// RIGHT(text, n): the last n characters.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Right", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RightFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="RightFunction"/> class.</summary>
    public RightFunction()
        : base(name: "Right", sqlName: "RIGHT", fewestArguments: 2, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        var count = call.Whole(1);
        if (text.IsFailure || count.IsFailure)
            return FirstFailure.Of(text, count);

        return text.Value is null || count.Value is not { } n ? SqlFunctionCall.Ok(null) : TextFunctions.Right(call, text.Value, n);
    }
}
