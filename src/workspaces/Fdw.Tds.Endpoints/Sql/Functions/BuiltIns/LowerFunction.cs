using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// LOWER(text): the text in lower case.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Lower", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LowerFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="LowerFunction"/> class.</summary>
    public LowerFunction()
        : base(name: "Lower", sqlName: "LOWER", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        if (text.IsFailure || text.Value is null)
            return text.ToNewResult<object?>();

        return SqlFunctionCall.Ok(text.Value.ToLowerInvariant());
    }
}
