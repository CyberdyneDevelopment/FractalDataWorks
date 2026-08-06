using System;
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
/// Translator for the FindUsagesCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindUsages")]
public sealed class FindUsagesTranslator : RoslynCommandTranslatorBase<FindUsagesCommand, QueryResult<IReadOnlyList<UsageInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindUsagesTranslator"/> class.
    /// </summary>
    public FindUsagesTranslator()
        : base("FindUsages", "Finds all references to a symbol")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve symbol, find references, build usage results
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<UsageInfo>>>> Translate(
        FindUsagesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.FilePath))
        {
            return GenericResult<QueryResult<IReadOnlyList<UsageInfo>>>.Failure(
                RoslynResultCodes.ByName("FilePathRequired"));
        }

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            return GenericResult<QueryResult<IReadOnlyList<UsageInfo>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            return GenericResult<QueryResult<IReadOnlyList<UsageInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            return GenericResult<QueryResult<IReadOnlyList<UsageInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var token = syntaxRoot.FindToken(command.Position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is null)
        {
            return GenericResult<QueryResult<IReadOnlyList<UsageInfo>>>.Failure(
                RoslynResultCodes.ByName("NoSymbolFoundAtOffset"),
                ResultDetails.Create().With("Position", command.Position));
        }

        // SymbolFinder.FindReferencesAsync throws on projects with unresolved
        // analyzer references; treat that as "no usages found" rather than
        // surfacing as a 500.
        List<UsageInfo> usages;
#pragma warning disable CA1031 // Defensive catch — see comment above
#pragma warning disable FDW014 // Why: SymbolFinder throws on unresolved analyzer refs; deliberate
                              // fallback to empty usages list keeps the scan best-effort. Surfacing
                              // the exception would require logger injection (out of scope here).
        try
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            usages = references
                .SelectMany(r => r.Locations)
                .Select(loc => new UsageInfo(
                    loc.Document.FilePath ?? string.Empty,
                    loc.Location.GetLineSpan().StartLinePosition.Line + 1,
                    loc.Location.GetLineSpan().StartLinePosition.Character + 1))
                .ToList();
        }
        catch (Exception ex)
        {
            // Why: per-call failures are tolerated (best-effort); record so they are visible.
            usages =
            [
                new UsageInfo(
                    $"[Error: FindReferencesAsync — {ex.GetType().Name}: {ex.Message}]",
                    0,
                    0),
            ];
        }
#pragma warning restore FDW014
#pragma warning restore CA1031

        if (command.IncludeDeclaration && symbol.Locations.Length > 0)
        {
            var declLoc = symbol.Locations[0];
            var lineSpan = declLoc.GetLineSpan();
            usages.Insert(0, new UsageInfo(
                lineSpan.Path ?? string.Empty,
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.StartLinePosition.Character + 1,
                true));
        }

        var summary = $"Found {usages.Count} usages of '{symbol.Name}'";
        var result = new QueryResult<IReadOnlyList<UsageInfo>>(summary, usages);

        return GenericResult<QueryResult<IReadOnlyList<UsageInfo>>>.Success(result, summary);
    }
#pragma warning restore MA0051
}
