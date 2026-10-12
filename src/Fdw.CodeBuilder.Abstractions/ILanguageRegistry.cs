using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.CodeBuilder.Abstractions;

/// <summary>
/// Registry for managing language-specific parsers.
/// </summary>
public interface ILanguageRegistry
{
    /// <summary>
    /// Gets the supported languages.
    /// </summary>
    IReadOnlyList<string> SupportedLanguages { get; }

    /// <summary>
    /// Checks if a language is supported.
    /// </summary>
    /// <param name="language">The language to check.</param>
    /// <returns>True if the language is supported.</returns>
    bool IsSupported(string language);

    /// <summary>
    /// Gets the file extensions associated with a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The file extensions.</returns>
    IReadOnlyList<string> GetExtensions(string language);

    /// <summary>
    /// Gets the language for a file extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without dot).</param>
    /// <returns>The language name, or null if not found.</returns>
    string? GetLanguageByExtension(string extension);

    /// <summary>
    /// Gets a parser for the specified language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The parser, or null if not available.</returns>
    Task<ICodeParser?> GetParser(string language, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a parser for a language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <param name="parser">The parser.</param>
    /// <param name="extensions">The file extensions for this language.</param>
    void RegisterParser(string language, ICodeParser parser, params string[] extensions);
}
