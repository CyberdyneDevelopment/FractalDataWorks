using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@SPID: the connection's session id.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "Spid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SpidVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="SpidVariable"/> class.</summary>
    public SpidVariable()
        : base(name: "Spid", sqlName: "@@SPID")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => (short)context.Session.SessionId;
}
