using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// RTRIM(text): the text without trailing spaces.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "RTrim", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RTrimFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="RTrimFunction"/> class.</summary>
    public RTrimFunction()
        : base(name: "RTrim", sqlName: "RTRIM", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var text = call.Text(0);
        if (text.IsFailure || text.Value is null)
            return text.ToNewResult<object?>();

        return SqlFunctionCall.Ok(text.Value.TrimEnd(' '));
    }
}
