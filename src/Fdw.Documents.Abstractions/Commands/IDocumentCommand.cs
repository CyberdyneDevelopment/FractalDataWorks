using Fdw.Abstractions;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>A command a document connection runs (FDW-1497). Its paths are relative to the scope of the connection.</summary>
public interface IDocumentCommand : IGenericCommand
{
    /// <summary>Lists what is wrong with the command itself, before any storage is reached.</summary>
    /// <returns>The first problem in words, or null when the command is sound.</returns>
    string? FirstProblem();
}

/// <summary>A document command whose type parameter carries the result type, so a caller's Execute infers it.</summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public interface IDocumentCommand<TResult> : IDocumentCommand
{
    // Marker: the type parameter carries the result type, so a caller's Execute infers it.
}
