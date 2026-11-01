using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// OBJECT_ID(name[, type]): the id of a table the caller may read; NULL for anything else.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "ObjectId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ObjectIdFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="ObjectIdFunction"/> class.</summary>
    public ObjectIdFunction()
        : base(name: "ObjectId", sqlName: "OBJECT_ID", fewestArguments: 1, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var name = call.Text(0);
        if (name.IsFailure)
            return name.ToNewResult<object?>();

        var type = call.Arguments.Count > 1 ? call.Text(1) : GenericResult<string?>.Success(null);
        if (type.IsFailure)
            return type.ToNewResult<object?>();

        return CatalogLookup.ObjectId(call, name.Value, type.Value);
    }
}
