using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// USER_ID(): the id of the database user the caller acts as.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "UserId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UserIdFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="UserIdFunction"/> class.</summary>
    public UserIdFunction()
        : base(name: "UserId", sqlName: "USER_ID", fewestArguments: 0, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        // 1 to 4 are dbo, guest, INFORMATION_SCHEMA and sys; the caller is the first ordinary user.
        return SqlFunctionCall.Ok(call.Arguments.Count == 0 ? 5 : null);
    }
}
