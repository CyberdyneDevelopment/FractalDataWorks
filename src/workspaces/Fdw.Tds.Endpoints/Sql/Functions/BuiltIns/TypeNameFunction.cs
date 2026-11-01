using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// TYPE_NAME(id): the name of the type a user_type_id names.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "TypeName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeNameFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="TypeNameFunction"/> class.</summary>
    public TypeNameFunction()
        : base(name: "TypeName", sqlName: "TYPE_NAME", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var id = call.Whole(0);
        return id.IsFailure ? id.ToNewResult<object?>() : SqlFunctionCall.Ok(CatalogLookup.TypeName(id.Value));
    }
}
