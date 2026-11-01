using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// ISNULL(value, replacement): the value, or the replacement when the value is NULL; the replacement is evaluated only then.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "IsNull", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IsNullFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="IsNullFunction"/> class.</summary>
    public IsNullFunction()
        : base(name: "IsNull", sqlName: "ISNULL", fewestArguments: 2, mostArguments: 2)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var value = call.Value(0);
        if (value.IsFailure || value.Value is not null)
            return value;

        return call.Value(1);
    }
}
