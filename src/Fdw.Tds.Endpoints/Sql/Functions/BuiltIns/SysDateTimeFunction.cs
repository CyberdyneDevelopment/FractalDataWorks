using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SYSDATETIME(): the server's local date and time when the request began.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SysDateTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SysDateTimeFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SysDateTimeFunction"/> class.</summary>
    public SysDateTimeFunction()
        : base(name: "SysDateTime", sqlName: "SYSDATETIME", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.RequestTime.LocalDateTime);
    }
}
