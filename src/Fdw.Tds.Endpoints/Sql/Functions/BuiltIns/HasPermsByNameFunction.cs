using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// HAS_PERMS_BY_NAME(securable, class, permission): 1 for a permission that reads, 0 for one that changes anything — this server only reads.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "HasPermsByName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HasPermsByNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="HasPermsByNameFunction"/> class.</summary>
    public HasPermsByNameFunction()
        : base(name: "HasPermsByName", sqlName: "HAS_PERMS_BY_NAME", fewestArguments: 3, mostArguments: 5)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var permission = call.Text(2);
        if (permission.IsFailure)
            return permission.ToNewResult<object?>();

        return SqlFunctionCall.Ok(permission.Value is { } name && ReadPermissions.Holds(name) ? 1 : 0);
    }
}
