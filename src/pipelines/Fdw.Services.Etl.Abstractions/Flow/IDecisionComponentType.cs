using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// A component type whose components decide (FDW-6134): they move no rows and fire outputs. A stored line that leaves an output of a decision is a signal line: the component
/// at its end runs only when the output fired (a gate reads the line as a value instead).
/// </summary>
/// <remarks>
/// Why a marker on the type and not a rule on the output's name: a component type's own outputs are named by the person for some kinds (a split's outputs, a switch's outcomes),
/// so no name can say a line is a signal; the kind of the component the line leaves can.
/// </remarks>
public interface IDecisionComponentType : IComponentType
{
    /// <summary>Lists the outputs a component of this kind fires from, given the settings the component states.</summary>
    /// <param name="stated">The text a setting is stated with, or <see langword="null"/> when the component does not state it.</param>
    /// <returns>The output names, in the order a designer lists them; empty when the component does not state what they come from.</returns>
    IReadOnlyList<string> FiredOutputs(Func<string, string?> stated);
}
