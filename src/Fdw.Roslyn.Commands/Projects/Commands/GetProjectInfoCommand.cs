using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Projects.Commands;

/// <summary>
/// Command to get detailed information about a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetProjectInfo")]
public sealed class GetProjectInfoCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetProjectInfoCommand"/> class.
    /// </summary>
    public GetProjectInfoCommand()
        : base("GetProjectInfo", RoslynCommandCategories.Project, "Return detailed information about a project: target framework, output type, language version, references, document list, output paths. Use to inspect a project's configuration without parsing the csproj. Returns a ProjectInfoResult object.")
    {
    }

    /// <summary>
    /// Gets or sets the name of the project.
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;
}
