using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to get compilation options for a project.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GetCompilationOptions")]
public sealed class GetCompilationOptionsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCompilationOptionsCommand"/> class.
    /// </summary>
    public GetCompilationOptionsCommand()
        : base("GetCompilationOptions", RoslynCommandCategories.Compilation, "Return the compilation options for ProjectName: language version, optimization level, unsafe-allowed flag, target framework, etc. Use to verify a project's compile settings before making language-feature decisions. Returns CompilationOptions metadata.")
    {
    }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    [System.ComponentModel.Description("Name of the project whose compilation options are requested.")]
    public string ProjectName { get; init; } = string.Empty;
}
