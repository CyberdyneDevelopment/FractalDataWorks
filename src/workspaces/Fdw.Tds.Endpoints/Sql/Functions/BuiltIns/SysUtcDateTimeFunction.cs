using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SYSUTCDATETIME(): the UTC date and time when the request began.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SysUtcDateTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysUtcDateTimeFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SysUtcDateTimeFunction"/> class.</summary>
    public SysUtcDateTimeFunction()
        : base(name: "SysUtcDateTime", sqlName: "SYSUTCDATETIME", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.RequestTime.UtcDateTime);
    }
}
