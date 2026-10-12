using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@LOCK_TIMEOUT: the lock timeout in milliseconds.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "LockTimeout", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LockTimeoutVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="LockTimeoutVariable"/> class.</summary>
    public LockTimeoutVariable()
        : base(name: "LockTimeout", sqlName: "@@LOCK_TIMEOUT")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Session.LockTimeout;
}
