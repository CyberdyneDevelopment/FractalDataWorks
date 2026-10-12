using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// NEWID(): a new uniqueidentifier.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "NewId", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NewIdFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="NewIdFunction"/> class.</summary>
    public NewIdFunction()
        : base(name: "NewId", sqlName: "NEWID", fewestArguments: 0, mostArguments: 0)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        return SqlFunctionCall.Ok(Guid.NewGuid());
    }
}
