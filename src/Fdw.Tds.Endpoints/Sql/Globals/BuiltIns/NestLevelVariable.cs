using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@NESTLEVEL: the procedure nesting depth.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "NestLevel", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NestLevelVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="NestLevelVariable"/> class.</summary>
    public NestLevelVariable()
        : base(name: "NestLevel", sqlName: "@@NESTLEVEL")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.NestLevel;
}
