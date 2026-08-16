using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Navigation.Commands;
using Fdw.Roslyn.Commands.Navigation.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Navigation.Translators;

/// <summary>
/// Translator for FindOverrides command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindOverridesTranslator")]
public sealed class FindOverridesTranslator : RoslynCommandTranslatorBase<FindOverridesCommand, QueryResult<IReadOnlyList<OverrideInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindOverridesTranslator"/> class.
    /// </summary>
    public FindOverridesTranslator()
        : base("FindOverridesTranslator", "Translates FindOverrides command to find method overrides")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<OverrideInfo>>>> Translate(
        FindOverridesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindOverridesTranslatorLog.Finding(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FindOverridesTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<OverrideInfo>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FindOverridesTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<OverrideInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            FindOverridesTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<OverrideInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not IMethodSymbol and not IPropertySymbol and not IEventSymbol)
        {
            FindOverridesTranslatorLog.SymbolMustBeMethodPropertyOrEvent(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<IReadOnlyList<OverrideInfo>>>.Failure(
                RoslynResultCodes.ByName("SymbolMustBeMethodPropertyOrEvent"));
        }

        var overrides = new List<OverrideInfo>();

        var foundOverrides = await SymbolFinder.FindOverridesAsync(
            symbol, solution, cancellationToken: cancellationToken).ConfigureAwait(false);

        foreach (var over in foundOverrides)
        {
            if (over.Locations.Length > 0 && over.Locations[0].IsInSource)
            {
                var lineSpan = over.Locations[0].GetLineSpan();
                overrides.Add(new OverrideInfo
                {
                    Name = over.Name,
                    ContainingType = over.ContainingType?.Name ?? string.Empty,
                    FullName = over.ToDisplayString(),
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1
                });
            }
        }

        var result = new QueryResult<IReadOnlyList<OverrideInfo>>(
            $"Found {overrides.Count} override(s) for '{symbol.Name}'",
            overrides);

        FindOverridesTranslatorLog.Found(Logger, symbol.Name, overrides.Count);

        return GenericResult<QueryResult<IReadOnlyList<OverrideInfo>>>.Success(result);
    }
}
