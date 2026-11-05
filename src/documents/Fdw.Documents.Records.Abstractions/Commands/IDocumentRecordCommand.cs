using Fdw.Abstractions;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>
/// A command a DocumentRecord connection carries out (FDW-3673): one operation of the document record layer, which each connection
/// implementation turns into its own storage's work.
/// </summary>
public interface IDocumentRecordCommand : IGenericCommand
{
    /// <summary>Says what is wrong with the command before it is sent, or null when nothing is.</summary>
    /// <returns>The first problem, or null.</returns>
    string? FirstProblem();
}

/// <summary>A document record command that answers with a result of one type.</summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public interface IDocumentRecordCommand<TResult> : IDocumentRecordCommand
{
    // Marker: the type parameter carries the result type, so a caller's Execute infers it.
}
