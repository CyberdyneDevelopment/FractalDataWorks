using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// USER_NAME(): the database user the caller acts as, which is its login.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "UserName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="UserNameFunction"/> class.</summary>
    public UserNameFunction()
        : base(name: "UserName", sqlName: "USER_NAME", fewestArguments: 0, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        if (call.Arguments.Count > 0)
            return SqlFunctionCall.Ok(null);

        return SqlFunctionCall.Ok(call.Context.Session.UserName);
    }
}
