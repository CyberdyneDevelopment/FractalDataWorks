using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// ORIGINAL_LOGIN(): the login the connection signed in with.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "OriginalLogin", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class OriginalLoginFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="OriginalLoginFunction"/> class.</summary>
    public OriginalLoginFunction()
        : base(name: "OriginalLogin", sqlName: "ORIGINAL_LOGIN", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(call.Context.Session.UserName);
    }
}
