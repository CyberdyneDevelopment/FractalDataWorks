using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// The documents of a project that a conventions scan reports on: the source files, each with its path.
/// </summary>
internal static class SourceDocuments
{
    /// <summary>
    /// Yields each document that has a file path, paired with that path.
    /// </summary>
    /// <remarks>
    /// A document with no file path is in memory, not a source file to report, so it is skipped and never
    /// named by something else (such as its document name).
    /// </remarks>
    /// <param name="documents">The project's documents.</param>
    /// <returns>Each source document and its file path.</returns>
    public static IEnumerable<(Document Document, string Path)> Of(IEnumerable<Document> documents)
    {
        foreach (var document in documents)
        {
            if (document.FilePath is { } path)
            {
                yield return (document, path);
            }
        }
    }
}
