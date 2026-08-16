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
/// Translator for FindBaseTypes command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindBaseTypesTranslator")]
public sealed class FindBaseTypesTranslator : RoslynCommandTranslatorBase<FindBaseTypesCommand, QueryResult<IReadOnlyList<TypeInfoResult>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindBaseTypesTranslator"/> class.
    /// </summary>
    public FindBaseTypesTranslator()
        : base("FindBaseTypesTranslator", "Translates FindBaseTypes command to find base types and interfaces")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>> Translate(
        FindBaseTypesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindBaseTypesTranslatorLog.Finding(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            FindBaseTypesTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            FindBaseTypesTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            FindBaseTypesTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            FindBaseTypesTranslatorLog.SymbolNotType(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("SymbolNotType"));
        }

        var baseTypes = new List<TypeInfoResult>();

        // Add base type
        if (typeSymbol.BaseType is not null && typeSymbol.BaseType.SpecialType != SpecialType.System_Object)
        {
            baseTypes.Add(CreateTypeInfo(typeSymbol.BaseType, "BaseClass"));
        }

        // Add interfaces
        if (command.IncludeInterfaces)
        {
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                baseTypes.Add(CreateTypeInfo(iface, "Interface"));
            }
        }

        var result = new QueryResult<IReadOnlyList<TypeInfoResult>>(
            $"Found {baseTypes.Count} base type(s) for '{typeSymbol.Name}'",
            baseTypes);

        FindBaseTypesTranslatorLog.Found(Logger, typeSymbol.Name, baseTypes.Count);

        return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Success(result);
    }

    private static TypeInfoResult CreateTypeInfo(INamedTypeSymbol type, string relationship)
    {
        var info = new TypeInfoResult
        {
            Name = type.Name,
            FullName = type.ToDisplayString(),
            Relationship = relationship
        };

        if (type.Locations.Length > 0 && type.Locations[0].IsInSource)
        {
            var lineSpan = type.Locations[0].GetLineSpan();
            return info with
            {
                FilePath = lineSpan.Path ?? string.Empty,
                Line = lineSpan.StartLinePosition.Line + 1,
                Column = lineSpan.StartLinePosition.Character + 1
            };
        }

        return info;
    }
}
