using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// OBJECT_SCHEMA_NAME(id): the schema of the table an id names; NULL for any other id.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "ObjectSchemaName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ObjectSchemaNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="ObjectSchemaNameFunction"/> class.</summary>
    public ObjectSchemaNameFunction()
        : base(name: "ObjectSchemaName", sqlName: "OBJECT_SCHEMA_NAME", fewestArguments: 1, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var id = call.Whole(0);
        return id.IsFailure ? id.ToNewResult<object?>() : CatalogLookup.ObjectName(call, id.Value, schema: true);
    }
}
