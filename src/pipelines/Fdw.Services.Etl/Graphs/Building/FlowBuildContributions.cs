using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Building;

/// <summary>
/// The build contributions the host registered (FDW-6132), found by their type: what a builder's <c>Needs</c> is judged against, at build and when
/// the host starts.
/// </summary>
/// <remarks>
/// A contribution is found by its own type, the one a builder names. The container keeps one registration of each implementation type, so each type
/// is here at most once.
/// </remarks>
public sealed class FlowBuildContributions
{
    private readonly Dictionary<Type, IFlowBuildContribution> _byType;

    /// <summary>Initializes a new instance of the <see cref="FlowBuildContributions"/> class.</summary>
    /// <param name="contributions">Every contribution the host registered.</param>
    public FlowBuildContributions(IEnumerable<IFlowBuildContribution> contributions)
    {
        _byType = [];
        foreach (var contribution in contributions)
        {
            _ = _byType.TryAdd(contribution.GetType(), contribution);
        }
    }

    /// <summary>Gets the type of every contribution held.</summary>
    public IReadOnlyCollection<Type> Types => _byType.Keys;

    /// <summary>Gets the contributions of a host that registered none.</summary>
    public static FlowBuildContributions None { get; } = new([]);

    /// <summary>Lists the needed contribution types the host did not register, in the order they are needed.</summary>
    /// <param name="needs">The contribution types a builder needs.</param>
    /// <returns>The types not registered; empty when every one is.</returns>
    public IReadOnlyList<Type> Unmet(IReadOnlyList<Type> needs) => [.. needs.Where(need => !_byType.ContainsKey(need))];

    /// <summary>Finds the contribution of a type.</summary>
    /// <typeparam name="T">The contribution type.</typeparam>
    /// <returns>The contribution, or <see langword="null"/> when the host registered none of that type.</returns>
    public T? Find<T>()
        where T : class, IFlowBuildContribution =>
        _byType.TryGetValue(typeof(T), out var contribution) ? (T)contribution : null;
}
