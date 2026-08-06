using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Navigation.Commands;
using Fdw.Roslyn.Commands.Navigation.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Navigation.Translators;

/// <summary>
/// Translator for GetContainingType command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetContainingTypeTranslator")]
public sealed class GetContainingTypeTranslator : RoslynCommandTranslatorBase<GetContainingTypeCommand, QueryResult<IReadOnlyList<TypeInfoResult>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetContainingTypeTranslator"/> class.
    /// </summary>
    public GetContainingTypeTranslator()
        : base("GetContainingTypeTranslator", "Translates GetContainingType command to get enclosing type")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: walk syntax tree upward, collect containing types
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>> Translate(
        GetContainingTypeCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));

        // Find the containing type by walking up the syntax tree
        var node = syntaxRoot.FindNode(new TextSpan(position, 0));
        var containingTypes = new List<TypeInfoResult>();

        while (node is not null)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
            if (declaredSymbol is INamedTypeSymbol typeSymbol)
            {
                var typeInfo = new TypeInfoResult
                {
                    Name = typeSymbol.Name,
                    FullName = typeSymbol.ToDisplayString(),
                    TypeKind = typeSymbol.TypeKind.ToString(),
                    Accessibility = typeSymbol.DeclaredAccessibility.ToString()
                };

                if (typeSymbol.Locations.Length > 0)
                {
                    var lineSpan = typeSymbol.Locations[0].GetLineSpan();
                    typeInfo = typeInfo with
                    {
                        FilePath = lineSpan.Path ?? string.Empty,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1
                    };
                }

                containingTypes.Add(typeInfo);

                if (!command.IncludeNested)
                    break;
            }

            node = node.Parent;
        }

        if (containingTypes.Count == 0)
            return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Failure(
                RoslynResultCodes.ByName("NoContainingTypeFoundAtPosition"));

        var primaryType = containingTypes[0].Name;
        var result = new QueryResult<IReadOnlyList<TypeInfoResult>>(
            $"Found containing type '{primaryType}'",
            containingTypes);

        return GenericResult<QueryResult<IReadOnlyList<TypeInfoResult>>>.Success(result);
    }
#pragma warning restore MA0051
}
