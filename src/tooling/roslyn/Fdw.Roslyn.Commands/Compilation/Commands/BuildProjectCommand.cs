using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to build a project and report results.
/// </summary>
[TypeOption(typeof(RoslynCommands), "BuildProject")]
public sealed class BuildProjectCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildProjectCommand"/> class.
    /// </summary>
    public BuildProjectCommand()
        : base("BuildProject", RoslynCommandCategories.Compilation, "Build the project named ProjectName and report the resulting diagnostics. Use before pack/publish or to verify a refactor didn't regress compilation. Returns BuildResult with success flag, diagnostic list, and elapsed time.")
    {
    }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    [System.ComponentModel.Description("Name of the target project as it appears in the solution.")]
    public string ProjectName { get; init; } = string.Empty;
}
