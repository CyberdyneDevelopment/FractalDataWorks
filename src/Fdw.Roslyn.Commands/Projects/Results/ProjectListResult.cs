using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Contains information about projects in a solution.
/// </summary>
public sealed class ProjectListResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectListResult"/> class.
    /// </summary>
    public ProjectListResult(int projectCount, IReadOnlyList<ProjectSummary> projects)
    {
        ProjectCount = projectCount;
        Projects = projects ?? throw new ArgumentNullException(nameof(projects));
    }

    /// <summary>
    /// Gets the total number of projects.
    /// </summary>
    public int ProjectCount { get; }

    /// <summary>
    /// Gets the list of projects.
    /// </summary>
    public IReadOnlyList<ProjectSummary> Projects { get; }
}