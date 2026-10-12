using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// GETUTCDATE(): the UTC date and time when the request began.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "GetUtcDate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GetUtcDateFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="GetUtcDateFunction"/> class.</summary>
    public GetUtcDateFunction()
        : base(name: "GetUtcDate", sqlName: "GETUTCDATE", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.RequestTime.UtcDateTime);
    }
}
