using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// HOST_NAME(): the client machine name the client sent at login.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "HostName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HostNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="HostNameFunction"/> class.</summary>
    public HostNameFunction()
        : base(name: "HostName", sqlName: "HOST_NAME", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.Session.HostName);
    }
}
