using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A name the pipeline does not declare.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SourceParameterProblems), "Undeclared")]
public sealed class UndeclaredSourceParameterProblem : SourceParameterProblemBase
{
    /// <summary>Initializes a new instance of the <see cref="UndeclaredSourceParameterProblem"/> class.</summary>
    public UndeclaredSourceParameterProblem()
        : base("Undeclared")
    {
    }

    /// <inheritdoc />
    public override bool Matches(bool isAssignedVariable, bool late) => !isAssignedVariable;

    /// <inheritdoc />
    public override string Reason(string component, string setting, string name) => $"component '{component}' names '{name}' in its {setting} setting, but the pipeline declares no parameter or variable '{name}'";
}
