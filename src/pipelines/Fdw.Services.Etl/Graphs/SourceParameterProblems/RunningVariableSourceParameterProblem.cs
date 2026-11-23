using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A variable a component sets while the run goes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SourceParameterProblems), "RunningVariable")]
public sealed class RunningVariableSourceParameterProblem : SourceParameterProblemBase
{
    /// <summary>Initializes a new instance of the <see cref="RunningVariableSourceParameterProblem"/> class.</summary>
    public RunningVariableSourceParameterProblem()
        : base("RunningVariable")
    {
    }

    /// <inheritdoc />
    public override bool Matches(bool isAssignedVariable, bool late) => isAssignedVariable && !late;

    /// <inheritdoc />
    public override string Reason(string component, string setting, string name) => $"component '{component}' names the variable '{name}' in its {setting} setting, but a component sets '{name}' while the run goes, and this setting is read when the run starts, so that component would be ignored; name a parameter or a variable no component sets (a source's physical path and name are read when its stage starts, so they may name a variable a component of a lower stage sets)";
}
