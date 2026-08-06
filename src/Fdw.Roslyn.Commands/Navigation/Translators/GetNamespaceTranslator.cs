using System;
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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Navigation.Translators;

/// <summary>
/// Translator for GetNamespace command.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetNamespaceTranslator")]
public sealed class GetNamespaceTranslator : RoslynCommandTranslatorBase<GetNamespaceCommand, QueryResult<NamespaceInfo>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetNamespaceTranslator"/> class.
    /// </summary>
    public GetNamespaceTranslator()
        : base("GetNamespaceTranslator", "Translates GetNamespace command to get containing namespace")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: walk syntax tree for namespace, check file-scoped fallback
    public override async Task<IGenericResult<QueryResult<NamespaceInfo>>> Translate(
        GetNamespaceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<NamespaceInfo>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<NamespaceInfo>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (syntaxRoot is null)
            return GenericResult<QueryResult<NamespaceInfo>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxRoot"));

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));

        // Find namespace by walking up the syntax tree
        var node = syntaxRoot.FindNode(new TextSpan(position, 0));
        NamespaceInfo? namespaceInfo = null;

        while (node is not null)
        {
            if (node is BaseNamespaceDeclarationSyntax nsDecl)
            {
                var namespaceName = nsDecl.Name.ToString();
                var lineSpan = nsDecl.GetLocation().GetLineSpan();
                namespaceInfo = new NamespaceInfo
                {
                    Name = namespaceName,
                    IsFileScopedNamespace = nsDecl is FileScopedNamespaceDeclarationSyntax,
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1
                };
                break;
            }

            node = node.Parent;
        }

        // If no namespace found, check for file-scoped namespace at root
        if (namespaceInfo is null)
        {
            var fileScopedNs = syntaxRoot.DescendantNodes()
                .OfType<FileScopedNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            if (fileScopedNs is not null)
            {
                var namespaceName = fileScopedNs.Name.ToString();
                var lineSpan = fileScopedNs.GetLocation().GetLineSpan();
                namespaceInfo = new NamespaceInfo
                {
                    Name = namespaceName,
                    IsFileScopedNamespace = true,
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1
                };
            }
        }

        if (namespaceInfo is null)
            return GenericResult<QueryResult<NamespaceInfo>>.Failure(
                RoslynResultCodes.ByName("NoNamespaceFoundAtPosition"));

        var result = new QueryResult<NamespaceInfo>(
            $"Found namespace '{namespaceInfo.Name}'",
            namespaceInfo);

        return GenericResult<QueryResult<NamespaceInfo>>.Success(result);
    }
#pragma warning restore MA0051
}
