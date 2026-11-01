using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Globals.BuiltIns;

/// <summary>
/// @@VERSION: the server's version banner.
/// </summary>
[TypeOption(typeof(SqlGlobalVariables), "Version", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class VersionVariable : SqlGlobalVariableBase
{
    /// <summary>Initializes a new instance of the <see cref="VersionVariable"/> class.</summary>
    public VersionVariable()
        : base(name: "Version", sqlName: "@@VERSION")
    {
    }

    /// <inheritdoc/>
    public override object? Read(SqlExecutionContext context) => context.Server.VersionBanner();
}
