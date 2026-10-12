using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// TRIM(text): the text without leading or trailing spaces.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Trim", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TrimFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="TrimFunction"/> class.</summary>
    public TrimFunction()
        : base(name: "Trim", sqlName: "TRIM", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        if (text.IsFailure || text.Value is null)
            return text.ToNewResult<object?>();

        return SqlFunctionCall.Ok(text.Value.Trim(' '));
    }
}
