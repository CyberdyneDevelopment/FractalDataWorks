using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@IDENTITY: the last identity value inserted; nothing is ever inserted here.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "Identity", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class IdentityVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentityVariable"/> class.</summary>
    public IdentityVariable()
        : base(name: "Identity", sqlName: "@@IDENTITY")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => null;
}
