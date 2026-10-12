namespace Fdw.Roslyn.Commands.Workspace.Results;

/// <summary>
/// Data returned from baseline information operations.
/// </summary>
public sealed class BaselineData
{
    /// <summary>
    /// Gets or sets a value indicating whether a baseline has been set.
    /// </summary>
    public bool HasBaseline { get; init; }

    /// <summary>
    /// Gets or sets the number of projects in the baseline.
    /// </summary>
    public int ProjectCount { get; init; }

    /// <summary>
    /// Gets or sets the number of documents in the baseline.
    /// </summary>
    public int DocumentCount { get; init; }
}
