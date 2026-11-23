using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs;

/// <summary>Base class for the ways a setting read when a stage starts can name a parameter or variable wrongly.</summary>
public abstract class SourceParameterProblemBase : TypeOptionBase<SourceParameterProblemBase>, ISourceParameterProblem
{
    /// <summary>Initializes a new instance of the <see cref="SourceParameterProblemBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected SourceParameterProblemBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(bool isAssignedVariable, bool late);

    /// <inheritdoc />
    public abstract string Reason(string component, string setting, string name);
}
