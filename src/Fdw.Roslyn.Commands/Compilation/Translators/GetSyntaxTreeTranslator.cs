#pragma warning disable CA1305 // Specify IFormatProvider - code compilation uses invariant strings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Compilation.Commands;
using Fdw.Roslyn.Commands.Compilation.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for getting syntax tree.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetSyntaxTree")]
public sealed class GetSyntaxTreeTranslator
    : RoslynCommandTranslatorBase<GetSyntaxTreeCommand, QueryResult<SyntaxTreeData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSyntaxTreeTranslator"/> class.
    /// </summary>
    public GetSyntaxTreeTranslator()
        : base("GetSyntaxTreeTranslator", "Translates get syntax tree commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: get syntax tree, collect type and method info
    public override async Task<IGenericResult<QueryResult<SyntaxTreeData>>> Translate(
        GetSyntaxTreeCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<QueryResult<SyntaxTreeData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<QueryResult<SyntaxTreeData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxTree is null)
            return GenericResult<QueryResult<SyntaxTreeData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxTree"));

        var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

        // Collect structure information
        var namespaces = root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString())
            .ToList();

        var types = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Select(t => new TypeDeclaration
            {
                Name = t.Identifier.Text,
                Kind = t.Kind().ToString(),
                Line = t.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            })
            .ToList();

        var methods = root.DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Select(m => new MethodDeclaration
            {
                Name = m.Identifier.Text,
                ReturnType = m.ReturnType.ToString(),
                Line = m.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            })
            .ToList();

        var data = new SyntaxTreeData
        {
            FilePath = command.FilePath,
            LanguageVersion = ((CSharpParseOptions)syntaxTree.Options).LanguageVersion.ToString(),
            Namespaces = namespaces,
            Types = types,
            Methods = methods,
            NodeCount = root.DescendantNodes().Count(),
            TriviaCount = command.IncludeTrivia ? root.DescendantTrivia().Count() : null
        };

        var result = new QueryResult<SyntaxTreeData>(
            $"Retrieved syntax tree for {command.FilePath}",
            data);

        return GenericResult<QueryResult<SyntaxTreeData>>.Success(result);
    }
#pragma warning restore MA0051
}
