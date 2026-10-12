using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// CHARINDEX(find, text[, start]): the 1-based position of find in the text, or 0; matched without case.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "CharIndex", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CharIndexFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="CharIndexFunction"/> class.</summary>
    public CharIndexFunction()
        : base(name: "CharIndex", sqlName: "CHARINDEX", fewestArguments: 2, mostArguments: 3)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var find = call.Text(0);
        var text = call.Text(1);
        var start = call.Arguments.Count > 2 ? call.Whole(2) : GenericResult<long?>.Success(1);
        if (find.IsFailure || text.IsFailure || start.IsFailure)
            return FirstFailure.Of(find, text, start);

        if (find.Value is null || text.Value is null || start.Value is not { } from)
            return SqlFunctionCall.Ok(null);

        return SqlFunctionCall.Ok(TextFunctions.IndexOf(find.Value, text.Value, from));
    }
}
