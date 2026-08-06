using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the SearchSymbolsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "SearchSymbols")]
public sealed class SearchSymbolsTranslator : RoslynCommandTranslatorBase<SearchSymbolsCommand, QueryResult<IReadOnlyList<SymbolInfoResult>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchSymbolsTranslator"/> class.
    /// </summary>
    public SearchSymbolsTranslator()
        : base("SearchSymbols", "Searches symbols by name pattern across the solution")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<SymbolInfoResult>>>> Translate(
        SearchSymbolsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.Pattern))
        {
            return GenericResult<QueryResult<IReadOnlyList<SymbolInfoResult>>>.Failure(
                RoslynResultCodes.ByName("PatternRequired"));
        }

        var matches = new List<SymbolInfoResult>();

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                continue;

            var symbols = await SymbolFinder.FindSourceDeclarationsWithPatternAsync(
                project, command.Pattern, SymbolFilter.All, cancellationToken).ConfigureAwait(false);

            foreach (var symbol in symbols.Take(command.MaxResults - matches.Count))
            {
                if (matches.Count >= command.MaxResults)
                    break;

                if (symbol.Locations.Length > 0)
                {
                    var loc = symbol.Locations[0];
                    var lineSpan = loc.GetLineSpan();
                    matches.Add(new SymbolInfoResult(
                        symbol.Name,
                        symbol.ToDisplayString(),
                        symbol.Kind.ToString(),
                        lineSpan.Path ?? string.Empty,
                        lineSpan.StartLinePosition.Line + 1,
                        lineSpan.StartLinePosition.Character + 1));
                }
            }
        }

        var result = new QueryResult<IReadOnlyList<SymbolInfoResult>>(
            $"Found {matches.Count} symbols matching '{command.Pattern}'",
            matches);

        return GenericResult<QueryResult<IReadOnlyList<SymbolInfoResult>>>.Success(result);
    }
}
