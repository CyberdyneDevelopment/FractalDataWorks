using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// IS_SRVROLEMEMBER(role): 0, because a caller of this endpoint holds no server role.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "IsSrvRoleMember", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IsSrvRoleMemberFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="IsSrvRoleMemberFunction"/> class.</summary>
    public IsSrvRoleMemberFunction()
        : base(name: "IsSrvRoleMember", sqlName: "IS_SRVROLEMEMBER", fewestArguments: 1, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var role = call.Text(0);
        return role.IsFailure ? role.ToNewResult<object?>() : SqlFunctionCall.Ok(0);
    }
}
