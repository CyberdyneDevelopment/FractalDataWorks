using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// LTRIM(text): the text without leading spaces.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "LTrim", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LTrimFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="LTrimFunction"/> class.</summary>
    public LTrimFunction()
        : base(name: "LTrim", sqlName: "LTRIM", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        if (text.IsFailure || text.Value is null)
            return text.ToNewResult<object?>();

        return SqlFunctionCall.Ok(text.Value.TrimStart(' '));
    }
}
