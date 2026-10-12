using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Workspace.Commands;
/// <summary>
/// Command to get workspace information.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetWorkspaceInfo")]
public sealed class GetWorkspaceInfoCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetWorkspaceInfoCommand"/> class.
    /// </summary>
    public GetWorkspaceInfoCommand()
        : base("GetWorkspaceInfo", RoslynCommandCategories.Workspace, "Return a summary of the loaded workspace: solution path, project count, document count, last-modified time. Use as a quick check that the right workspace is loaded. Returns workspace metadata.")
    {
    }
}
