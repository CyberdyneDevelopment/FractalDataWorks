using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Tds.Endpoints.Hosting;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// DB_ID([name]): the current database's id, or the id of the database a name names.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "DbId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DbIdFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="DbIdFunction"/> class.</summary>
    public DbIdFunction()
        : base(name: "DbId", sqlName: "DB_ID", fewestArguments: 0, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        if (call.Arguments.Count == 0)
            return SqlFunctionCall.Ok(TdsServerIdentity.DatabaseId);

        var name = call.Text(0);
        if (name.IsFailure)
            return name.ToNewResult<object?>();

        return SqlFunctionCall.Ok(Databases.IdOf(name.Value, call.Context));
    }
}
