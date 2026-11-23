using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>One of the ways a compute stage routes an output's rows.</summary>
public interface IComputeOutputRoute : ITypeOption<int, ComputeOutputRouteBase>
{
    /// <summary>Says what is wrong with a stage that states this word with, or without, a name.</summary>
    /// <param name="named">Whether the stage states a name.</param>
    /// <returns>The problem, or <c>null</c> when the stage is right.</returns>
    string? ProblemWith(bool named);
}
