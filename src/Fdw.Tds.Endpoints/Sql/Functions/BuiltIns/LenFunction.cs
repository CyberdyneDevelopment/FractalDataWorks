using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// LEN(text): the number of characters, not counting trailing spaces.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Len", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LenFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="LenFunction"/> class.</summary>
    public LenFunction()
        : base(name: "Len", sqlName: "LEN", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        if (text.IsFailure || text.Value is null)
            return text.ToNewResult<object?>();

        return SqlFunctionCall.Ok(text.Value.TrimEnd(' ').Length);
    }
}
