using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// The ways a compute stage routes an output's rows: <c>Replace</c> (they replace the stage's own rows) and <c>Named</c> (they are kept under a name).
/// </summary>
/// <remarks>A stage states a route by the option's name, matched exactly through <c>ByName</c>; a word that matches none is a problem of the stage, said by <see cref="ProblemOf"/>.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ComputeOutputRouteBase), typeof(IComputeOutputRoute), typeof(ComputeOutputRoutes))]
public abstract partial class ComputeOutputRoutes : TypeCollectionBase<ComputeOutputRouteBase, IComputeOutputRoute>
{
    /// <summary>Says what is wrong with an output routed by <paramref name="route"/>, named or not.</summary>
    /// <param name="route">The route word the stage states.</param>
    /// <param name="named">Whether the output states the name it keeps its rows under.</param>
    /// <returns>The problem, or <c>null</c> when the output is right.</returns>
    public static string? ProblemOf(string? route, bool named)
        => (route is { } word ? ByName(word) : NotFound) is var kind && !ReferenceEquals(kind, NotFound)
            ? kind.ProblemWith(named)
            : $"'{route}' is not Replace or Named";
}
