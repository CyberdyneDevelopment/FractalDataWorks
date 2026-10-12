using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SCHEMA_ID([name]): the id of the caller's default schema, dbo, or of the schema a name names.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "SchemaId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SchemaIdFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="SchemaIdFunction"/> class.</summary>
    public SchemaIdFunction()
        : base(name: "SchemaId", sqlName: "SCHEMA_ID", fewestArguments: 0, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        if (call.Arguments.Count == 0)
            return SqlFunctionCall.Ok(Catalog.CatalogIds.Dbo);

        var name = call.Text(0);
        if (name.IsFailure)
            return name.ToNewResult<object?>();

        return CatalogLookup.SchemaId(call, name.Value);
    }
}
