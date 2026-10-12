using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@MICROSOFTVERSION: the version packed as major, minor and build.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "MicrosoftVersion", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MicrosoftVersionVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="MicrosoftVersionVariable"/> class.</summary>
    public MicrosoftVersionVariable()
        : base(name: "MicrosoftVersion", sqlName: "@@MICROSOFTVERSION")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Server.Version.MicrosoftVersion();
}
