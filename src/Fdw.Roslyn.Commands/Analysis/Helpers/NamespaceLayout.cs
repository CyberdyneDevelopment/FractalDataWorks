using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Helpers;

/// <summary>
/// Pure namespace/path/project reconciliation. Given a type's namespace, works out which project
/// should own it and where inside that project it should sit.
/// </summary>
/// <remarks>
/// The rule is <b>namespace is the source of truth</b>: a project owns a namespace when the project's
/// assembly name equals the namespace root, and the namespace segments below that root are the folder
/// segments beneath the project directory.
/// </remarks>
public static class NamespaceLayout
{
    private static readonly char[] NamespaceSeparator = { '.' };

    /// <summary>
    /// Gets the folder segments a type in <paramref name="namespaceName"/> should sit under, relative to
    /// the root of the project named <paramref name="projectName"/>.
    /// </summary>
    /// <param name="namespaceName">The type's namespace.</param>
    /// <param name="projectName">The name of the project that owns the namespace root.</param>
    /// <returns>
    /// The folder segments, empty when the namespace IS the project root, or <see langword="null"/> when
    /// the namespace does not sit under the project at all.
    /// </returns>
    public static IReadOnlyList<string>? RelativeFolders(string namespaceName, string projectName)
    {
        if (string.IsNullOrWhiteSpace(namespaceName) || string.IsNullOrWhiteSpace(projectName))
            return null;

        if (string.Equals(namespaceName, projectName, StringComparison.Ordinal))
            return Array.Empty<string>();

        if (!namespaceName.StartsWith(projectName + ".", StringComparison.Ordinal))
            return null;

        return namespaceName
            .Substring(projectName.Length + 1)
            .Split(NamespaceSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Computes the absolute path a type should occupy, given its namespace and the project that owns it.
    /// </summary>
    /// <param name="project">The project that should own the type.</param>
    /// <param name="namespaceName">The type's namespace.</param>
    /// <param name="typeName">The type's name (the file stem).</param>
    /// <returns>The expected absolute file path, or <see langword="null"/> when it cannot be derived.</returns>
    public static string? ExpectedPath(Project project, string namespaceName, string typeName)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var projectDirectory = ProjectDirectory(project);
        if (projectDirectory is null) return null;

        var folders = RelativeFolders(namespaceName, project.Name);
        if (folders is null) return null;

        var segments = new List<string>(folders.Count + 2) { projectDirectory };
        segments.AddRange(folders);
        segments.Add(typeName + ".cs");

        return Path.Combine(segments.ToArray());
    }

    /// <summary>
    /// Gets the directory containing a project's project-file.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <returns>The directory, or <see langword="null"/> when the project has no file path.</returns>
    public static string? ProjectDirectory(Project project)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));
        return string.IsNullOrEmpty(project.FilePath) ? null : Path.GetDirectoryName(project.FilePath);
    }

    /// <summary>
    /// Gets a document's path relative to its project directory — the type's position within its
    /// service tree.
    /// </summary>
    /// <param name="project">The project owning the document.</param>
    /// <param name="documentPath">The document's absolute path.</param>
    /// <returns>The relative path, or <see langword="null"/> when it cannot be derived.</returns>
    /// <remarks>
    /// Recorded into the ledger so a later split slice can check the programme's positional invariant —
    /// that the same component for each service lands in the same RELATIVE project — against what an
    /// earlier slice actually did.
    /// </remarks>
    public static string? RelativePosition(Project project, string? documentPath)
    {
        if (string.IsNullOrEmpty(documentPath)) return null;

        var projectDirectory = ProjectDirectory(project);
        if (projectDirectory is null) return null;

        if (!documentPath!.StartsWith(projectDirectory, StringComparison.Ordinal))
            return null;

        return documentPath
            .Substring(projectDirectory.Length)
            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace('\\', '/');
    }

    /// <summary>
    /// Determines whether two paths refer to the same location.
    /// </summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns><see langword="true"/> when both normalise to the same path.</returns>
    public static bool SamePath(string? left, string? right)
    {
        if (left is null || right is null) return false;
        return string.Equals(Normalise(left), Normalise(right), StringComparison.Ordinal);
    }

    private static string Normalise(string path) =>
        path.Replace('\\', '/').TrimEnd('/');
}
