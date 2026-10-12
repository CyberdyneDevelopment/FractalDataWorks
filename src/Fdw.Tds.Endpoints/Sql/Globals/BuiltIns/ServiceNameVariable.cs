using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@SERVICENAME: the service name of a default instance.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "ServiceName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServiceNameVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="ServiceNameVariable"/> class.</summary>
    public ServiceNameVariable()
        : base(name: "ServiceName", sqlName: "@@SERVICENAME")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => "MSSQLSERVER";
}
