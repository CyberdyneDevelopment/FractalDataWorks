using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Represents a mutation result that modifies the solution.
/// </summary>
public class MutationResult : IRoslynCommandResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutationResult"/> class.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="newSolution">The new solution after mutation.</param>
    public MutationResult(string summary, Solution newSolution)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        NewSolution = newSolution ?? throw new ArgumentNullException(nameof(newSolution));
        ChangedFiles = Array.Empty<FileChange>();
        SymbolChanges = Array.Empty<SymbolChange>();
        PathChanges = Array.Empty<PathChange>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutationResult"/> class with file changes.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="newSolution">The new solution after mutation.</param>
    /// <param name="changedFiles">The list of changed files.</param>
    public MutationResult(string summary, Solution newSolution, IReadOnlyList<FileChange> changedFiles)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        NewSolution = newSolution ?? throw new ArgumentNullException(nameof(newSolution));
        ChangedFiles = changedFiles ?? throw new ArgumentNullException(nameof(changedFiles));
        SymbolChanges = Array.Empty<SymbolChange>();
        PathChanges = Array.Empty<PathChange>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutationResult"/> class with file, symbol,
    /// and path changes.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="newSolution">The new solution after mutation.</param>
    /// <param name="changedFiles">The list of changed files.</param>
    /// <param name="symbolChanges">The list of symbol-level changes.</param>
    /// <param name="pathChanges">The list of path changes.</param>
    public MutationResult(
        string summary,
        Solution newSolution,
        IReadOnlyList<FileChange> changedFiles,
        IReadOnlyList<SymbolChange> symbolChanges,
        IReadOnlyList<PathChange> pathChanges)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        NewSolution = newSolution ?? throw new ArgumentNullException(nameof(newSolution));
        ChangedFiles = changedFiles ?? throw new ArgumentNullException(nameof(changedFiles));
        SymbolChanges = symbolChanges ?? throw new ArgumentNullException(nameof(symbolChanges));
        PathChanges = pathChanges ?? throw new ArgumentNullException(nameof(pathChanges));
    }

    /// <summary>
    /// Gets a summary of the result.
    /// </summary>
    public string Summary { get; }

    /// <summary>
    /// Gets a value indicating whether this result represents a mutation.
    /// </summary>
    public bool IsMutation => true;

    /// <summary>
    /// Gets the new solution if this is a mutation result, otherwise null.
    /// </summary>
    /// <remarks>
    /// Excluded from JSON serialization because Microsoft.CodeAnalysis.Solution transitively
    /// owns SourceText / Encoding objects that contain Span&lt;byte&gt; properties (Encoding.Preamble),
    /// which System.Text.Json cannot serialize. The Solution is still accessible in-process
    /// for downstream commands (e.g. ApplyWorkspaceChanges) that need to commit the mutation.
    /// </remarks>
    [JsonIgnore]
    public Solution NewSolution { get; }

    [JsonIgnore]
    Solution? IRoslynCommandResult.NewSolution => NewSolution;

    /// <summary>
    /// Gets the list of changed files.
    /// </summary>
    public IReadOnlyList<FileChange> ChangedFiles { get; }

    /// <summary>
    /// Gets the list of symbol-level changes (renames, moves, additions, removals).
    /// </summary>
    public IReadOnlyList<SymbolChange> SymbolChanges { get; }

    /// <summary>
    /// Gets the list of path changes (project directories, .csproj references, .slnx entries).
    /// </summary>
    public IReadOnlyList<PathChange> PathChanges { get; }
}

/// <summary>
/// Represents a typed mutation result with additional data.
/// </summary>
/// <typeparam name="T">The type of additional data.</typeparam>
// Why: pure result/warning POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class MutationResult<T> : MutationResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MutationResult{T}"/> class.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="newSolution">The new solution after mutation.</param>
    /// <param name="data">Additional result data.</param>
    public MutationResult(string summary, Solution newSolution, T data)
        : base(summary, newSolution)
    {
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutationResult{T}"/> class with file changes.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="newSolution">The new solution after mutation.</param>
    /// <param name="changedFiles">The list of changed files.</param>
    /// <param name="data">Additional result data.</param>
    public MutationResult(string summary, Solution newSolution, IReadOnlyList<FileChange> changedFiles, T data)
        : base(summary, newSolution, changedFiles)
    {
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MutationResult{T}"/> class with file, symbol,
    /// and path changes.
    /// </summary>
    /// <param name="summary">A summary of the result.</param>
    /// <param name="newSolution">The new solution after mutation.</param>
    /// <param name="changedFiles">The list of changed files.</param>
    /// <param name="symbolChanges">The list of symbol-level changes.</param>
    /// <param name="pathChanges">The list of path changes.</param>
    /// <param name="data">Additional result data.</param>
    public MutationResult(
        string summary,
        Solution newSolution,
        IReadOnlyList<FileChange> changedFiles,
        IReadOnlyList<SymbolChange> symbolChanges,
        IReadOnlyList<PathChange> pathChanges,
        T data)
        : base(summary, newSolution, changedFiles, symbolChanges, pathChanges)
    {
        Data = data;
    }

    /// <summary>
    /// Gets the additional result data.
    /// </summary>
    public T Data { get; }
}
