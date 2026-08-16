using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for retrieving type hierarchy.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetTypeHierarchy")]
public sealed class GetTypeHierarchyTranslator
    : RoslynCommandTranslatorBase<GetTypeHierarchyCommand, QueryResult<TypeHierarchyData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetTypeHierarchyTranslator"/> class.
    /// </summary>
    public GetTypeHierarchyTranslator()
        : base("GetTypeHierarchyTranslator", "Translates type hierarchy retrieval commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve type, walk base types, collect interfaces
    public override async Task<IGenericResult<QueryResult<TypeHierarchyData>>> Translate(
        GetTypeHierarchyCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        GetTypeHierarchyTranslatorLog.Retrieving(Logger, command.FilePath, command.Line, command.Column, command.IncludeInterfaces);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            GetTypeHierarchyTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<TypeHierarchyData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            GetTypeHierarchyTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<TypeHierarchyData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            GetTypeHierarchyTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<TypeHierarchyData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            GetTypeHierarchyTranslatorLog.SymbolNotType(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<TypeHierarchyData>>.Failure(
                RoslynResultCodes.ByName("SymbolNotType"));
        }

        var baseTypes = new List<TypeHierarchyEntry>();

        // Build base type hierarchy
        var current = typeSymbol.BaseType;
        var depth = 0;
        while (current is not null && !string.Equals(current.Name, "Object", StringComparison.Ordinal))
        {
            baseTypes.Add(CreateTypeInfo(current, "BaseType", depth));
            current = current.BaseType;
            depth++;
        }

        // Add interfaces if requested
        var interfaces = new List<TypeHierarchyEntry>();
        if (command.IncludeInterfaces)
        {
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                interfaces.Add(CreateTypeInfo(iface, "Interface", 0));
            }
        }

        var data = new TypeHierarchyData
        {
            TypeName = typeSymbol.ToDisplayString(),
            BaseTypes = baseTypes,
            Interfaces = interfaces,
            BaseTypeCount = baseTypes.Count,
            InterfaceCount = interfaces.Count
        };

        var result = new QueryResult<TypeHierarchyData>(
            $"Retrieved hierarchy for '{typeSymbol.Name}': {baseTypes.Count} base types, {interfaces.Count} interfaces",
            data);

        GetTypeHierarchyTranslatorLog.Retrieved(Logger, typeSymbol.ToDisplayString(), baseTypes.Count, interfaces.Count);

        return GenericResult<QueryResult<TypeHierarchyData>>.Success(result);
    }
#pragma warning restore MA0051

    private static TypeHierarchyEntry CreateTypeInfo(INamedTypeSymbol type, string relationship, int depth)
    {
        var entry = new TypeHierarchyEntry
        {
            Name = type.Name,
            FullName = type.ToDisplayString(),
            Relationship = relationship,
            Depth = depth,
            TypeKind = type.TypeKind.ToString(),
            Namespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty
        };

        if (type.Locations.Length > 0 && type.Locations[0].IsInSource)
        {
            var lineSpan = type.Locations[0].GetLineSpan();
            entry = entry with
            {
                FilePath = lineSpan.Path ?? string.Empty,
                Line = lineSpan.StartLinePosition.Line + 1
            };
        }

        return entry;
    }
}
