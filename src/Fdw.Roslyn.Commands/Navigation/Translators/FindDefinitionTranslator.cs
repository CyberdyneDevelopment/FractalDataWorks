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
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Navigation.Translators;

/// <summary>
/// Translator for FindDefinition command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindDefinitionTranslator")]
public sealed class FindDefinitionTranslator : RoslynCommandTranslatorBase<FindDefinitionCommand, QueryResult<IReadOnlyList<SymbolLocationInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDefinitionTranslator"/> class.
    /// </summary>
    public FindDefinitionTranslator()
        : base("FindDefinitionTranslator", "Translates FindDefinition command to navigate to symbol definitions")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>> Translate(
        FindDefinitionCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindDefinitionTranslatorLog.Finding(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FindDefinitionTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FindDefinitionTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            FindDefinitionTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is null)
        {
            FindDefinitionTranslatorLog.NoSymbolFoundAtLineColumn(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>.Failure(
                RoslynResultCodes.ByName("NoSymbolFoundAtLineColumn"),
                ResultDetails.Create().With("Line", command.Line).With("Column", command.Column));
        }

        var definitions = symbol.Locations
            .Where(l => l.IsInSource)
            .Select(l => l.GetLineSpan())
            .Select(ls => new SymbolLocationInfo
            {
                FilePath = ls.Path ?? string.Empty,
                Line = ls.StartLinePosition.Line + 1,
                Column = ls.StartLinePosition.Character + 1
            })
            .ToList();

        if (definitions.Count == 0)
        {
            FindDefinitionTranslatorLog.NoSourceDefinitionFound(Logger, symbol.Name);
            return GenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>.Failure(
                RoslynResultCodes.ByName("NoSourceDefinitionFound"),
                ResultDetails.Create().With("SymbolName", symbol.Name));
        }

        var result = new QueryResult<IReadOnlyList<SymbolLocationInfo>>(
            $"Found {definitions.Count} definition(s) for '{symbol.Name}'",
            definitions);

        FindDefinitionTranslatorLog.Found(Logger, symbol.Name, definitions.Count);

        return GenericResult<QueryResult<IReadOnlyList<SymbolLocationInfo>>>.Success(result);
    }
}
