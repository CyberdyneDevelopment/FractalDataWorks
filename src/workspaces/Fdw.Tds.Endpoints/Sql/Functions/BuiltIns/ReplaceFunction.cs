using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// REPLACE(text, find, with): the text with every occurrence of find replaced, matched without case as the collation compares.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "Replace", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ReplaceFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="ReplaceFunction"/> class.</summary>
    public ReplaceFunction()
        : base(name: "Replace", sqlName: "REPLACE", fewestArguments: 3, mostArguments: 3)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        var find = call.Text(1);
        var with = call.Text(2);
        if (text.IsFailure || find.IsFailure || with.IsFailure)
            return FirstFailure.Of(text, find, with);

        if (text.Value is null || find.Value is null || with.Value is null)
            return SqlFunctionCall.Ok(null);

        return SqlFunctionCall.Ok(find.Value.Length == 0 ? text.Value : text.Value.Replace(find.Value, with.Value, StringComparison.OrdinalIgnoreCase));
    }
}
