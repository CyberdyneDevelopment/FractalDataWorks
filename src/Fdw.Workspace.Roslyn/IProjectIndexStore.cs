using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Provides storage for project session indices.
/// </summary>
/// <remarks>
/// <para>
/// Project session indices are stored in .claude/roslyn.sessions within
/// project directories. These lightweight files track which sessions
/// belong to a project.
/// </para>
/// </remarks>
public interface IProjectIndexStore
{
    /// <summary>
    /// Loads a project session index.
    /// </summary>
    /// <param name="projectPath">The project directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project session index, or null if not found.</returns>
    Task<ProjectSessionIndex?> LoadIndex(
        string projectPath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a project session index.
    /// </summary>
    /// <param name="projectPath">The project directory path.</param>
    /// <param name="index">The index to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult<bool>> SaveIndex(
        string projectPath,
        ProjectSessionIndex index,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the file path for a project's session index.
    /// </summary>
    /// <param name="projectPath">The project directory path.</param>
    /// <returns>The full file path for the session index.</returns>
    string GetIndexPath(string projectPath);

    /// <summary>
    /// Checks if a project has a session index.
    /// </summary>
    /// <param name="projectPath">The project directory path.</param>
    /// <returns>True if the index exists.</returns>
    bool IndexExists(string projectPath);
}