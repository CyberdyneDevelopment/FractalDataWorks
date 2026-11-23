using Fdw.Collections;

namespace Fdw.Services.Etl.Graphs;

/// <summary>A way a setting that is read when a stage starts can name a parameter or variable wrongly, and how it is told in words.</summary>
public interface ISourceParameterProblem : ITypeOption<int, SourceParameterProblemBase>
{
    /// <summary>Tells whether this is the problem of a name with these facts.</summary>
    /// <param name="isAssignedVariable">Whether the name is a variable some component sets while the run goes.</param>
    /// <param name="late">Whether no component of a lower stage sets the variable before this setting is read.</param>
    /// <returns><c>true</c> when it is.</returns>
    bool Matches(bool isAssignedVariable, bool late);

    /// <summary>Words the problem for a graph's refusal.</summary>
    /// <param name="component">The component's name.</param>
    /// <param name="setting">The setting that names the parameter or variable.</param>
    /// <param name="name">The name it states.</param>
    /// <returns>The sentence.</returns>
    string Reason(string component, string setting, string name);
}
