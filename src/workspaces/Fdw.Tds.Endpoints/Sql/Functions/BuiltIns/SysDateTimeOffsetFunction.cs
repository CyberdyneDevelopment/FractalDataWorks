using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SYSDATETIMEOFFSET(): the date, time and offset when the request began.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SysDateTimeOffset", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysDateTimeOffsetFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SysDateTimeOffsetFunction"/> class.</summary>
    public SysDateTimeOffsetFunction()
        : base(name: "SysDateTimeOffset", sqlName: "SYSDATETIMEOFFSET", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.RequestTime);
    }
}
