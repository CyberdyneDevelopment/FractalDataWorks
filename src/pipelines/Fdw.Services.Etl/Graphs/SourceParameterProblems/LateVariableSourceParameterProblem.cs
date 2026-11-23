using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A variable no component of a lower stage sets.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SourceParameterProblems), "LateVariable")]
public sealed class LateVariableSourceParameterProblem : SourceParameterProblemBase
{
    /// <summary>Initializes a new instance of the <see cref="LateVariableSourceParameterProblem"/> class.</summary>
    public LateVariableSourceParameterProblem()
        : base("LateVariable")
    {
    }

    /// <inheritdoc />
    public override bool Matches(bool isAssignedVariable, bool late) => isAssignedVariable && late;

    /// <inheritdoc />
    public override string Reason(string component, string setting, string name) => $"component '{component}' names the variable '{name}' in its {setting} setting, but no component of a lower stage sets '{name}', so it would have only its starting value; give the component that sets '{name}' a lower stage than this one";
}
