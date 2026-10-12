using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SUSER_NAME(): the caller's login name.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SuserName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuserNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SuserNameFunction"/> class.</summary>
    public SuserNameFunction()
        : base(name: "SuserName", sqlName: "SUSER_NAME", fewestArguments: 0, mostArguments: 1)
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
