using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Search.Commands;

/// <summary>Scripts that don't define any object (CREATE / ALTER / DROP) — leftover comments, settings, or no-ops.</summary>
[TypeOption(typeof(SqlCommands), "FindOrphanScripts", RestrictToCurrentCompilation = true)]
public sealed class FindOrphanScriptsCommand : SqlCommandBase
{
    public FindOrphanScriptsCommand() : base("FindOrphanScripts", StandardSqlCommandCategories.Search,
        "List scripts that contain no CREATE / ALTER / DROP — likely leftover comment blocks, settings files, or no-ops the project no longer needs.") { }
}
