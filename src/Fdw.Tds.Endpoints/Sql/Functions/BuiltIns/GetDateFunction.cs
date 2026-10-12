using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// GETDATE(): the server's local date and time when the request began.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "GetDate", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GetDateFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="GetDateFunction"/> class.</summary>
    public GetDateFunction()
        : base(name: "GetDate", sqlName: "GETDATE", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.RequestTime.LocalDateTime);
    }
}
