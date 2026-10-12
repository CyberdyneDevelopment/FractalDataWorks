using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// QUOTENAME(name[, quote]): the name delimited with brackets, or with the quote character given.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "QuoteName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class QuoteNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="QuoteNameFunction"/> class.</summary>
    public QuoteNameFunction()
        : base(name: "QuoteName", sqlName: "QUOTENAME", fewestArguments: 1, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var name = call.Text(0);
        var quote = call.Arguments.Count > 1 ? call.Text(1) : GenericResult<string?>.Success("[");
        if (name.IsFailure || quote.IsFailure)
            return FirstFailure.Of(name, quote);

        return name.Value is null || quote.Value is null ? SqlFunctionCall.Ok(null) : SqlFunctionCall.Ok(TextFunctions.Quote(name.Value, quote.Value));
    }
}
