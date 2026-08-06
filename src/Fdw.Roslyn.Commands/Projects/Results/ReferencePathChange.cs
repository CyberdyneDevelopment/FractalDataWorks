using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// A single ProjectReference Include path change within a .csproj file.
/// </summary>
public sealed class ReferencePathChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReferencePathChange"/> class.
    /// </summary>
    public ReferencePathChange(string oldInclude, string newInclude)
    {
        OldInclude = oldInclude ?? throw new ArgumentNullException(nameof(oldInclude));
        NewInclude = newInclude ?? throw new ArgumentNullException(nameof(newInclude));
    }

    /// <summary>
    /// Gets the original ProjectReference Include path.
    /// </summary>
    public string OldInclude { get; }

    /// <summary>
    /// Gets the new ProjectReference Include path.
    /// </summary>
    public string NewInclude { get; }
}
