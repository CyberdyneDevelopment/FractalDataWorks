using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// APP_NAME(): the application name the client sent at login.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "AppName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AppNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="AppNameFunction"/> class.</summary>
    public AppNameFunction()
        : base(name: "AppName", sqlName: "APP_NAME", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.Session.ApplicationName);
    }
}
