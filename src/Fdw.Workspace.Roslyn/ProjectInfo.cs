#pragma warning disable MA0016 // Prefer using collection abstraction - DTOs need concrete types for serialization

using System;
using System.Collections.Generic;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Represents information about a project in a solution.
/// </summary>
public sealed record ProjectInfo
{
    /// <summary>
    /// Gets the project name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the absolute path to the project file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets whether the project is currently loaded in the workspace.
    /// </summary>
    public bool IsLoaded { get; init; }

    /// <summary>
    /// Gets whether the project was excluded by a pattern.
    /// </summary>
    public bool IsExcluded { get; init; }

    /// <summary>
    /// Gets the pattern that excluded this project, if any.
    /// </summary>
    public string? ExcludedByPattern { get; init; }

    /// <summary>
    /// Gets whether this project appears to be a test project.
    /// </summary>
    public bool IsTestProject { get; init; }

    /// <summary>
    /// Gets the project type (e.g., "C#", "VB", "F#").
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Gets the names of projects this project references.
    /// </summary>
    public List<string> ProjectReferences { get; init; } = [];

    /// <summary>
    /// Gets the names of projects that reference this project.
    /// </summary>
    public List<string> ReferencedBy { get; init; } = [];
}