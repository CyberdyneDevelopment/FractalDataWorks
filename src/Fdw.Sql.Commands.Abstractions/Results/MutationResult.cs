using System;
using System.Collections.Generic;

namespace Fdw.Sql.Commands.Abstractions.Results;

/// <summary>
/// Mutation result. The script edits already live in the workspace's in-memory
/// state; <see cref="ChangedScripts"/> just reports which paths were touched so
/// the caller can size the eventual <c>ApplyWorkspaceChanges</c> commit.
/// </summary>
public class MutationResult : ISqlCommandResult
{
    public MutationResult(string summary, IReadOnlyList<string> changedScripts)
    {
        Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        ChangedScripts = changedScripts ?? Array.Empty<string>();
    }

    /// <inheritdoc/>
    public string Summary { get; }

    /// <inheritdoc/>
    public bool IsMutation => true;

    /// <summary>The .sql file paths whose in-memory text changed.</summary>
    public IReadOnlyList<string> ChangedScripts { get; }
}

/// <summary>Mutation result with an additional typed payload (e.g. the new object metadata).</summary>
// Why: pure result/warning POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class MutationResult<T> : MutationResult
{
    public MutationResult(string summary, IReadOnlyList<string> changedScripts, T data)
        : base(summary, changedScripts)
    {
        Data = data;
    }

    public T Data { get; }
}
