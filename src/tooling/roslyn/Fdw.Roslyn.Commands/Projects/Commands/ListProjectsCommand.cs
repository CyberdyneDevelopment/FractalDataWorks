using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;
/// <summary>
/// Command to list all projects in the solution.
/// </summary>
[TypeOption(typeof(RoslynCommands), "ListProjects")]
public sealed class ListProjectsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListProjectsCommand"/> class.
    /// </summary>
    public ListProjectsCommand()
        : base("ListProjects", RoslynCommandCategories.Project, "List all projects in the loaded solution. Use as a first orientation step — counts and project list let downstream commands target by ProjectName. Returns each project's name, file path, language, document count, and output kind.")
    {
    }
}
