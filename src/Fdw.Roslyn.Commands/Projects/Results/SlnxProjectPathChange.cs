using System;

namespace Fdw.Roslyn.Commands.Projects.Results;

/// <summary>
/// A single Project Path change within the .slnx file.
/// </summary>
public sealed class SlnxProjectPathChange
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SlnxProjectPathChange"/> class.
    /// </summary>
    public SlnxProjectPathChange(string oldPath, string newPath)
    {
        OldPath = oldPath ?? throw new ArgumentNullException(nameof(oldPath));
        NewPath = newPath ?? throw new ArgumentNullException(nameof(newPath));
    }

    /// <summary>
    /// Gets the original .slnx project path.
    /// </summary>
    public string OldPath { get; }

    /// <summary>
    /// Gets the new .slnx project path.
    /// </summary>
    public string NewPath { get; }
}
