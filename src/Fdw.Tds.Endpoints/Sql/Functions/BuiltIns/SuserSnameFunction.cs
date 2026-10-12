using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SUSER_SNAME(): the caller's login name.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SuserSname", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SuserSnameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SuserSnameFunction"/> class.</summary>
    public SuserSnameFunction()
        : base(name: "SuserSname", sqlName: "SUSER_SNAME", fewestArguments: 0, mostArguments: 1)
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
