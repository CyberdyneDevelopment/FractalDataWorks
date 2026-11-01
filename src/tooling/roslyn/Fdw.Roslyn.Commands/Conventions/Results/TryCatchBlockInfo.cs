using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// Information about a try-catch block.
/// </summary>
public sealed class TryCatchBlockInfo
{
    /// <summary>
    /// Gets or sets the list of catch types.
    /// </summary>
    public required IReadOnlyList<string> CatchTypes { get; init; }

    /// <summary>
    /// Gets or sets the count of catch clauses.
    /// </summary>
    public required int CatchCount { get; init; }

    /// <summary>
    /// Gets or sets whether the block has a finally clause.
    /// </summary>
    public required bool HasFinally { get; init; }

    /// <summary>
    /// Gets or sets the method name.
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the containing type.
    /// </summary>
    public required string ContainingType { get; init; }

    /// <summary>
    /// Gets or sets the project name.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public required int Line { get; init; }
}