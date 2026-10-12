using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Build.Commands;

/// <summary>Build the loaded .sqlproj via DacFx and report success/failures.</summary>
[TypeOption(typeof(SqlCommands), "BuildProject", RestrictToCurrentCompilation = true)]
public sealed class BuildProjectCommand : SqlCommandBase
{
    public BuildProjectCommand() : base("BuildProject", StandardSqlCommandCategories.Build,
        "Build the loaded .sqlproj via DacFx. Returns success/error counts and the diagnostics list.") { }
}
