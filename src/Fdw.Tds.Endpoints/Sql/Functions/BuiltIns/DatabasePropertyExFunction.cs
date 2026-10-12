using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// DATABASEPROPERTYEX(database, property): a property of the one database this server holds; NULL for any other database or an unknown property.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "DatabasePropertyEx", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DatabasePropertyExFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="DatabasePropertyExFunction"/> class.</summary>
    public DatabasePropertyExFunction()
        : base(name: "DatabasePropertyEx", sqlName: "DATABASEPROPERTYEX", fewestArguments: 2, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var database = call.Text(0);
        if (database.IsFailure)
            return database.ToNewResult<object?>();

        var property = call.Text(1);
        if (property.IsFailure)
            return property.ToNewResult<object?>();

        var ours = string.Equals(database.Value, call.Context.Server.DatabaseName, StringComparison.OrdinalIgnoreCase);
        return SqlFunctionCall.Ok(ours && property.Value is { } name && DatabaseProperties.TryRead(name, call.Context, out var value) ? value : null);
    }
}
