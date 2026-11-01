using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// IS_MEMBER(role): 1 for public, which every user belongs to, and 0 for any other role.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "IsMember", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IsMemberFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="IsMemberFunction"/> class.</summary>
    public IsMemberFunction()
        : base(name: "IsMember", sqlName: "IS_MEMBER", fewestArguments: 1, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var role = call.Text(0);
        if (role.IsFailure)
            return role.ToNewResult<object?>();

        return SqlFunctionCall.Ok(string.Equals(role.Value, "public", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
    }
}
