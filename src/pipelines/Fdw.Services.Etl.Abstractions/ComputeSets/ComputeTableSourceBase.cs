using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>Base class for the places a compute stage's table takes its rows from.</summary>
public abstract class ComputeTableSourceBase : TypeOptionBase<ComputeTableSourceBase>, IComputeTableSource
{
    private readonly bool _requiresName;
    private readonly string _problem;

    /// <summary>Initializes a new instance of the <see cref="ComputeTableSourceBase"/> class.</summary>
    /// <param name="name">The option name, the word a stage states.</param>
    /// <param name="requiresName">Whether a stage that states this word has to state a name with it.</param>
    /// <param name="problem">What is wrong when the name is stated or left out against that rule.</param>
    protected ComputeTableSourceBase(string name, bool requiresName, string problem)
        : base(name)
    {
        _requiresName = requiresName;
        _problem = problem;
    }

    /// <inheritdoc />
    public string? ProblemWith(bool named) => named == _requiresName ? null : _problem;
}
