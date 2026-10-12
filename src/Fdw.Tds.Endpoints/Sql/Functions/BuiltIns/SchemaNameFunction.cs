using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SCHEMA_NAME([id]): the caller's default schema, dbo, or the name of the schema an id names.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SchemaName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SchemaNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SchemaNameFunction"/> class.</summary>
    public SchemaNameFunction()
        : base(name: "SchemaName", sqlName: "SCHEMA_NAME", fewestArguments: 0, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        if (call.Arguments.Count == 0)
            return SqlFunctionCall.Ok("dbo");

        var id = call.Whole(0);
        if (id.IsFailure)
            return id.ToNewResult<object?>();

        return CatalogLookup.SchemaName(call, id.Value);
    }
}
