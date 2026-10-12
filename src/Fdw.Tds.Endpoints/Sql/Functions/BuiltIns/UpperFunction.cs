using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// UPPER(text): the text in upper case.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Upper", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpperFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="UpperFunction"/> class.</summary>
    public UpperFunction()
        : base(name: "Upper", sqlName: "UPPER", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        if (text.IsFailure || text.Value is null)
            return text.ToNewResult<object?>();

        return SqlFunctionCall.Ok(text.Value.ToUpperInvariant());
    }
}
