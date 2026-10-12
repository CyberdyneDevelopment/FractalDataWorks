using System;
using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Compilation.Commands;

/// <summary>
/// Command to emit an assembly from compilation.
/// </summary>
[TypeOption(typeof(RoslynCommands), "EmitAssembly")]
public sealed class EmitAssemblyCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmitAssemblyCommand"/> class.
    /// </summary>
    public EmitAssemblyCommand()
        : base("EmitAssembly", RoslynCommandCategories.Compilation, "Emit the compiled assembly for ProjectName to OutputPath, optionally emitting the PDB (EmitPdb, default true). Use when you need a built DLL on disk without invoking the full dotnet build pipeline. Returns EmitResult with success flag and any emit-time diagnostics.")
    {
    }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    [System.ComponentModel.Description("Name of the project to compile and emit.")]
    public string ProjectName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the output assembly path.
    /// </summary>
    [System.ComponentModel.Description("Directory or file path where the resulting assembly is written.")]
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to emit PDB file.
    /// </summary>
    [System.ComponentModel.Description("When true (default), also emit a PDB symbol file next to the assembly.")]
    public bool EmitPdb { get; init; } = true;
}
