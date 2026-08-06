using System.Diagnostics.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>
/// The outcome of editing a project or props file.
/// </summary>
// Why: pure data holder with three factories, no logic
[ExcludeFromCodeCoverage]
public sealed class ProjectFileEditResult
{
    private ProjectFileEditResult(bool success, bool changed, string filePath, string detail)
    {
        Success = success;
        Changed = changed;
        FilePath = filePath;
        Detail = detail;
    }

    /// <summary>Gets a value indicating whether the edit succeeded.</summary>
    public bool Success { get; }

    /// <summary>Gets a value indicating whether anything was actually written.</summary>
    public bool Changed { get; }

    /// <summary>Gets the file the edit targeted.</summary>
    public string FilePath { get; }

    /// <summary>Gets a description of what was written, or why it failed.</summary>
    public string Detail { get; }

    /// <summary>Creates a result for a successful write.</summary>
    /// <param name="filePath">The file written.</param>
    /// <param name="detail">What was written.</param>
    /// <returns>The result.</returns>
    public static ProjectFileEditResult Written(string filePath, string detail) =>
        new(success: true, changed: true, filePath, detail);

    /// <summary>Creates a result for a reference that was already present.</summary>
    /// <param name="filePath">The file inspected.</param>
    /// <returns>The result.</returns>
    public static ProjectFileEditResult AlreadyPresent(string filePath) =>
        new(success: true, changed: false, filePath, "already present");

    /// <summary>Creates a failed result.</summary>
    /// <param name="detail">Why it failed.</param>
    /// <returns>The result.</returns>
    public static ProjectFileEditResult Failed(string detail) =>
        new(success: false, changed: false, string.Empty, detail);
}
