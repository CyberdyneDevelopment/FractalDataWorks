using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// Runs a command a person wrote and answers how many rows it affected (FDW-1435).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ScriptedDataCommands), "Run", RestrictToCurrentCompilation = true)]
public sealed class ScriptRunCommand : ScriptedDataCommandBase<int>, IScriptRunCommand
{
    /// <summary>Initializes a new instance of the <see cref="ScriptRunCommand"/> class.</summary>
    /// <param name="text">The command.</param>
    /// <param name="parameters">The values of its named parameters.</param>
    public ScriptRunCommand(string text, IReadOnlyList<ScriptParameter> parameters)
        : base("Run", text, parameters)
    {
    }
}
