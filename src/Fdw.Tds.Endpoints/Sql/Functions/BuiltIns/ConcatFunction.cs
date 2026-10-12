using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// CONCAT(value, value, ...): the values joined as text, a NULL joining as empty text.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Concat", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConcatFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="ConcatFunction"/> class.</summary>
    public ConcatFunction()
        : base(name: "Concat", sqlName: "CONCAT", fewestArguments: 2, mostArguments: 254)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var values = call.Values();
        if (values.IsFailure)
            return values.ToNewResult<object?>();

        var joined = new System.Text.StringBuilder();
        foreach (var value in values.Value!)
        {
            if (value is not null)
                joined.Append(SqlValues.ToText(value));
        }

        return SqlFunctionCall.Ok(joined.ToString());
    }
}
