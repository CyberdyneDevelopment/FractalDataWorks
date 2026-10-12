using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Tds.Endpoints.Sql.Functions.BuiltIns;

/// <summary>
/// SERVERPROPERTY(name): a property of the server this endpoint presents; NULL for a name SQL Server does not define, as SQL Server answers.
/// </summary>
[TypeOption(typeof(SqlScalarFunctions), "ServerProperty", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServerPropertyFunction : SqlScalarFunctionBase
{
    /// <summary>Initializes a new instance of the <see cref="ServerPropertyFunction"/> class.</summary>
    public ServerPropertyFunction()
        : base(name: "ServerProperty", sqlName: "SERVERPROPERTY", fewestArguments: 1, mostArguments: 1)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult<object?> Run(SqlFunctionCall call)
    {
        var name = call.Text(0);
        if (name.IsFailure)
            return name.ToNewResult<object?>();

        return SqlFunctionCall.Ok(name.Value is { } property && ServerProperties.TryRead(property, call.Context, out var value) ? value : null);
    }
}
