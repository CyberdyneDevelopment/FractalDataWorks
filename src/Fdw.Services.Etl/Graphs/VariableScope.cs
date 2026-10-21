using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The variables a step can see when a graph is checked (FDW-1733): the graph's variables and parameters and the variable of every
/// loop that holds the step.
/// </summary>
/// <remarks>
/// A loop whose variable is chosen by a run parameter has a name only the run knows, so the steps inside it cannot be judged for names
/// in scope when the graph is saved. The scope is then open: it claims no name is missing, and the same check runs when the run starts,
/// on the step with its real names.
/// </remarks>
internal sealed class VariableScope
{
    private readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets a value indicating whether a loop around the step takes its variable from a run parameter.</summary>
    public bool IsOpen { get; set; }

    /// <summary>Adds a variable.</summary>
    /// <param name="name">The variable's name.</param>
    public void Add(string name) => _names.Add(name);

    /// <summary>Whether a name is a variable the step can see.</summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when the scope is open, or the name has been added.</returns>
    public bool Contains(string name) => IsOpen || _names.Contains(name);
}
