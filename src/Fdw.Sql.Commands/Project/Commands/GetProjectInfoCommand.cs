using Fdw.Collections.Attributes;
using Fdw.Sql.Commands.Abstractions;

namespace Fdw.Sql.Commands.Project.Commands;

/// <summary>Return project-level metadata: target platform, script count, object counts per kind.</summary>
[TypeOption(typeof(SqlCommands), "GetProjectInfo", RestrictToCurrentCompilation = true)]
public sealed class GetProjectInfoCommand : SqlCommandBase
{
    public GetProjectInfoCommand()
        : base("GetProjectInfo", StandardSqlCommandCategories.Project,
               "Return project-level metadata: project path, target platform, script count, object counts per kind.")
    {
    }
}
