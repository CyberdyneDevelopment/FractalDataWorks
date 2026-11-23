using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>One of the places a compute stage's table takes its rows from.</summary>
public interface IComputeTableSource : ITypeOption<int, ComputeTableSourceBase>
{
    /// <summary>Says what is wrong with a stage that states this word with, or without, a name.</summary>
    /// <param name="named">Whether the stage states a name.</param>
    /// <returns>The problem, or <c>null</c> when the stage is right.</returns>
    string? ProblemWith(bool named);
}
