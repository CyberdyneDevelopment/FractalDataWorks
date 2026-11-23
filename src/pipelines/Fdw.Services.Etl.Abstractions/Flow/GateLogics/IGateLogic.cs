using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>How a gate turns the values of its input lines into its answer (FDW-6134).</summary>
public interface IGateLogic : ITypeOption<int, IGateLogic>
{
    /// <summary>Gets the fewest input lines a gate of this logic takes.</summary>
    int MinimumInputs { get; }

    /// <summary>Gets the most input lines a gate of this logic takes; <see langword="null"/> when it takes any number.</summary>
    int? MaximumInputs { get; }

    /// <summary>Judges the values of the input lines.</summary>
    /// <param name="inputs">The value of each input line, in the order of the lines.</param>
    /// <returns>Whether the gate's answer is true.</returns>
    bool Judge(IReadOnlyList<bool> inputs);
}
