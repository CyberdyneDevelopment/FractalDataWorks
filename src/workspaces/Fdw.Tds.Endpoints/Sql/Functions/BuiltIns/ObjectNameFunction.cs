using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// OBJECT_NAME(id): the name of the table an id names; NULL for any other id.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "ObjectName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ObjectNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="ObjectNameFunction"/> class.</summary>
    public ObjectNameFunction()
        : base(name: "ObjectName", sqlName: "OBJECT_NAME", fewestArguments: 1, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var id = call.Whole(0);
        return id.IsFailure ? id.ToNewResult<object?>() : CatalogLookup.ObjectName(call, id.Value, schema: false);
    }
}
