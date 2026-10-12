using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// Details of project path changes needed in the .slnx file.
/// </summary>
public sealed class SlnxChangeDetail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnxChangeDetail"/> class.
    /// </summary>
    public SlnxChangeDetail(string slnxPath, IReadOnlyList<SlnxProjectPathChange> projectPathChanges)
    {
        SlnxPath = slnxPath ?? throw new ArgumentNullException(nameof(slnxPath));
        ProjectPathChanges = projectPathChanges ?? throw new ArgumentNullException(nameof(projectPathChanges));
    }

    /// <summary>
    /// Gets the path to the .slnx file.
    /// </summary>
    public string SlnxPath { get; }

    /// <summary>
    /// Gets the list of project path changes.
    /// </summary>
    public IReadOnlyList<SlnxProjectPathChange> ProjectPathChanges { get; }
}
