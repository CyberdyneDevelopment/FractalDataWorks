using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// Answers whether adding a project reference would close a cycle.
/// </summary>
/// <remarks>
/// Roslyn will hold a cyclic project graph in memory without complaint; MSBuild will not build it. So a
/// move that adds the wrong reference produces a solution that looks fine in the tool and fails on the
/// command line — which is exactly the kind of gap this whole verification pass exists to close.
/// </remarks>
public static class ProjectReferenceCycle
{
    /// <summary>
    /// Determines whether referencing <paramref name="toProjectId"/> from
    /// <paramref name="fromProjectId"/> would create a cycle.
    /// </summary>
    /// <param name="solution">The solution.</param>
    /// <param name="fromProjectId">The project that would gain the reference.</param>
    /// <param name="toProjectId">The project that would be referenced.</param>
    /// <returns><see langword="true"/> when the target already reaches back to the consumer.</returns>
    /// <remarks>
    /// A cycle exists precisely when the target can already reach the consumer, because the new edge
    /// would close the loop. Self-reference counts.
    /// </remarks>
    public static bool WouldCreateCycle(Solution solution, ProjectId fromProjectId, ProjectId toProjectId)
    {
        if (solution is null) throw new ArgumentNullException(nameof(solution));
        if (fromProjectId is null) throw new ArgumentNullException(nameof(fromProjectId));
        if (toProjectId is null) throw new ArgumentNullException(nameof(toProjectId));

        if (fromProjectId == toProjectId) return true;

        return Reaches(solution, toProjectId, fromProjectId, new HashSet<ProjectId>());
    }

    /// <summary>
    /// Describes the path that closes the cycle, for a caller that has to plan around it.
    /// </summary>
    /// <param name="solution">The solution.</param>
    /// <param name="fromProjectId">The project that would gain the reference.</param>
    /// <param name="toProjectId">The project that would be referenced.</param>
    /// <returns>A readable chain, or <see langword="null"/> when no cycle would be created.</returns>
    public static string? DescribeCycle(Solution solution, ProjectId fromProjectId, ProjectId toProjectId)
    {
        if (!WouldCreateCycle(solution, fromProjectId, toProjectId)) return null;

        var from = solution.GetProject(fromProjectId)?.Name ?? "(unknown)";
        var to = solution.GetProject(toProjectId)?.Name ?? "(unknown)";

        var path = new List<ProjectId>();
        if (fromProjectId != toProjectId) FindPath(solution, toProjectId, fromProjectId, new HashSet<ProjectId>(), path);

        var hops = new List<string> { from, to };
        foreach (var id in path)
        {
            var name = solution.GetProject(id)?.Name;
            if (name is not null && !string.Equals(name, to, StringComparison.Ordinal)) hops.Add(name);
        }

        hops.Add(from);

        return string.Join(" -> ", hops);
    }

    private static bool Reaches(Solution solution, ProjectId start, ProjectId target, HashSet<ProjectId> seen)
    {
        if (start == target) return true;
        if (!seen.Add(start)) return false;

        var project = solution.GetProject(start);
        if (project is null) return false;

        foreach (var reference in project.ProjectReferences)
        {
            if (Reaches(solution, reference.ProjectId, target, seen)) return true;
        }

        return false;
    }

    private static bool FindPath(
        Solution solution,
        ProjectId start,
        ProjectId target,
        HashSet<ProjectId> seen,
        List<ProjectId> path)
    {
        if (!seen.Add(start)) return false;

        path.Add(start);
        if (start == target) return true;

        var project = solution.GetProject(start);
        if (project is not null)
        {
            foreach (var reference in project.ProjectReferences)
            {
                if (FindPath(solution, reference.ProjectId, target, seen, path)) return true;
            }
        }

        path.RemoveAt(path.Count - 1);
        return false;
    }
}
