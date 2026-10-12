using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// DB_NAME([id]): the current database's name, or the name of the database an id names.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "DbName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DbNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="DbNameFunction"/> class.</summary>
    public DbNameFunction()
        : base(name: "DbName", sqlName: "DB_NAME", fewestArguments: 0, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        if (call.Arguments.Count == 0)
            return SqlFunctionCall.Ok(call.Context.Server.DatabaseName);

        var id = call.Whole(0);
        if (id.IsFailure)
            return id.ToNewResult<object?>();

        return SqlFunctionCall.Ok(Databases.NameOf(id.Value, call.Context));
    }
}
