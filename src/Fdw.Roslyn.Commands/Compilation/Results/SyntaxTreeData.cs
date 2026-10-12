using System;
using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Compilation.Results;

/// <summary>
/// Data returned by get syntax tree operation.
/// </summary>
public sealed class SyntaxTreeData
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the language version.
    /// </summary>
    public required string LanguageVersion { get; init; }

    /// <summary>
    /// Gets or sets the namespaces.
    /// </summary>
    public required IReadOnlyList<string> Namespaces { get; init; }

    /// <summary>
    /// Gets or sets the types.
    /// </summary>
    public required IReadOnlyList<TypeDeclaration> Types { get; init; }

    /// <summary>
    /// Gets or sets the methods.
    /// </summary>
    public required IReadOnlyList<MethodDeclaration> Methods { get; init; }

    /// <summary>
    /// Gets or sets the node count.
    /// </summary>
    public required int NodeCount { get; init; }

    /// <summary>
    /// Gets or sets the trivia count (if requested).
    /// </summary>
    public int? TriviaCount { get; init; }
}