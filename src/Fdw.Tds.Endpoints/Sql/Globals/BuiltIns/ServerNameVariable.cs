using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@SERVERNAME: the server's name.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "ServerName", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServerNameVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="ServerNameVariable"/> class.</summary>
    public ServerNameVariable()
        : base(name: "ServerName", sqlName: "@@SERVERNAME")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Server.ServerName;
}
