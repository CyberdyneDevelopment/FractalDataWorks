using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@MAX_CONNECTIONS: the most connections SQL Server allows.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "MaxConnections", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MaxConnectionsVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="MaxConnectionsVariable"/> class.</summary>
    public MaxConnectionsVariable()
        : base(name: "MaxConnections", sqlName: "@@MAX_CONNECTIONS")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => 32767;
}
