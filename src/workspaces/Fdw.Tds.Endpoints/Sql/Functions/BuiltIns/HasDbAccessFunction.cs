using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// HAS_DBACCESS(database): 1 for the database this server holds, 0 for any other.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "HasDbAccess", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class HasDbAccessFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="HasDbAccessFunction"/> class.</summary>
    public HasDbAccessFunction()
        : base(name: "HasDbAccess", sqlName: "HAS_DBACCESS", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var database = call.Text(0);
        if (database.IsFailure)
            return database.ToNewResult<object?>();

        return SqlFunctionCall.Ok(string.Equals(database.Value, call.Context.Server.DatabaseName, StringComparison.OrdinalIgnoreCase) ? 1 : 0);
    }
}
