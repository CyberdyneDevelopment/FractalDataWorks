using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the SearchTextCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "SearchText")]
public sealed class SearchTextTranslator : RoslynCommandTranslatorBase<SearchTextCommand, QueryResult<IReadOnlyList<TextMatchInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchTextTranslator"/> class.
    /// </summary>
    public SearchTextTranslator()
        : base("SearchText", "Performs full-text search across source files")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: validate pattern, iterate documents, find text matches
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<TextMatchInfo>>>> Translate(
        SearchTextCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.Pattern))
        {
            SearchTextTranslatorLog.PatternRequired(Logger);
            return GenericResult<QueryResult<IReadOnlyList<TextMatchInfo>>>.Failure(
                RoslynResultCodes.ByName("PatternRequired"));
        }

        SearchTextTranslatorLog.Searching(Logger, command.Pattern, command.IsRegex, command.CaseSensitive, command.MaxResults);

        var matches = new List<TextMatchInfo>();
        var filesWithMatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Regex? regex = null;
        if (command.IsRegex)
        {
            try
            {
                var options = RegexOptions.Compiled;
                if (!command.CaseSensitive)
                    options |= RegexOptions.IgnoreCase;
                regex = new Regex(command.Pattern, options, TimeSpan.FromSeconds(5));
            }
            catch (ArgumentException ex)
            {
                SearchTextTranslatorLog.InvalidRegexPattern(Logger, command.Pattern, ex.Message);
                return GenericResult<QueryResult<IReadOnlyList<TextMatchInfo>>>.Failure(
                RoslynResultCodes.ByName("InvalidRegexPattern"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
            }
        }

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested || matches.Count >= command.MaxResults)
                break;

            foreach (var document in project.Documents)
            {
                if (cancellationToken.IsCancellationRequested || matches.Count >= command.MaxResults)
                    break;

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var text = sourceText.ToString();

                var documentMatches = FindMatches(text, command.Pattern, regex, command.CaseSensitive);

                foreach (var match in documentMatches)
                {
                    if (matches.Count >= command.MaxResults)
                        break;

                    var lineSpan = sourceText.Lines.GetLinePositionSpan(new TextSpan(match.Index, match.Length));
                    var filePath = document.FilePath ?? string.Empty;

                    filesWithMatches.Add(filePath);
                    matches.Add(new TextMatchInfo(
                        filePath,
                        lineSpan.Start.Line + 1,
                        lineSpan.Start.Character + 1,
                        match.Value,
                        sourceText.Lines[lineSpan.Start.Line].ToString().Trim()));
                }
            }
        }

        var result = new QueryResult<IReadOnlyList<TextMatchInfo>>(
            $"Found {matches.Count} matches in {filesWithMatches.Count} files",
            matches);

        SearchTextTranslatorLog.Found(Logger, matches.Count, filesWithMatches.Count);

        return GenericResult<QueryResult<IReadOnlyList<TextMatchInfo>>>.Success(result);
    }
#pragma warning restore MA0051

    private static IEnumerable<(int Index, int Length, string Value)> FindMatches(
        string text,
        string pattern,
        Regex? regex,
        bool caseSensitive)
    {
        if (regex is not null)
        {
            foreach (Match match in regex.Matches(text))
            {
                yield return (match.Index, match.Length, match.Value);
            }
        }
        else
        {
            var comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            var index = 0;
            while ((index = text.IndexOf(pattern, index, comparison)) >= 0)
            {
#if NETSTANDARD2_0
                yield return (index, pattern.Length, text.Substring(index, pattern.Length));
#else
                yield return (index, pattern.Length, text[index..(index + pattern.Length)]);
#endif
                index += pattern.Length;
            }
        }
    }
}
