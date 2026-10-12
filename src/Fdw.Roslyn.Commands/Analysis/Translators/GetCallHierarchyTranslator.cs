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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for building call hierarchy.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetCallHierarchy")]
public sealed class GetCallHierarchyTranslator
    : RoslynCommandTranslatorBase<GetCallHierarchyCommand, QueryResult<CallHierarchyData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCallHierarchyTranslator"/> class.
    /// </summary>
    public GetCallHierarchyTranslator()
        : base("GetCallHierarchyTranslator", "Translates call hierarchy commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<CallHierarchyData>>> Translate(
        GetCallHierarchyCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        GetCallHierarchyTranslatorLog.Building(Logger, command.FilePath, command.Line, command.Column, command.Direction, command.MaxDepth);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            GetCallHierarchyTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<QueryResult<CallHierarchyData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            GetCallHierarchyTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<CallHierarchyData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            GetCallHierarchyTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<QueryResult<CallHierarchyData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);
        var symbol = semanticModel.GetSymbolInfo(token.Parent!, cancellationToken).Symbol
                  ?? semanticModel.GetDeclaredSymbol(token.Parent!, cancellationToken);

        if (symbol is not IMethodSymbol methodSymbol)
        {
            GetCallHierarchyTranslatorLog.SymbolNotMethod(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<QueryResult<CallHierarchyData>>.Failure(
                RoslynResultCodes.ByName("SymbolNotMethod"));
        }

        var hierarchy = new List<CallHierarchyEntry>();

        if (string.Equals(command.Direction, "callers", StringComparison.OrdinalIgnoreCase))
        {
            await FindCallers(methodSymbol, solution, hierarchy, 0, command.MaxDepth, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await FindCallees(methodSymbol, solution, hierarchy, 0, command.MaxDepth, cancellationToken).ConfigureAwait(false);
        }

        var data = new CallHierarchyData
        {
            MethodName = methodSymbol.ToDisplayString(),
            Direction = command.Direction,
            Hierarchy = hierarchy,
            Count = hierarchy.Count
        };

        var result = new QueryResult<CallHierarchyData>(
            $"Built {command.Direction} hierarchy for '{methodSymbol.Name}' with {hierarchy.Count} entries",
            data);

        GetCallHierarchyTranslatorLog.Built(Logger, methodSymbol.ToDisplayString(), command.Direction, hierarchy.Count);

        return GenericResult<QueryResult<CallHierarchyData>>.Success(result);
    }

    private static async Task FindCallers(
        IMethodSymbol method,
        Solution solution,
        List<CallHierarchyEntry> results,
        int depth,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        if (depth >= maxDepth)
            return;

        var callers = await SymbolFinder.FindCallersAsync(method, solution, cancellationToken).ConfigureAwait(false);

        foreach (var caller in callers)
        {
            if (caller.CallingSymbol is IMethodSymbol callerMethod &&
                caller.Locations.Any())
            {
                var loc = caller.Locations.First();
                var lineSpan = loc.GetLineSpan();

                results.Add(new CallHierarchyEntry
                {
                    Name = callerMethod.Name,
                    FullName = callerMethod.ToDisplayString(),
                    ContainingType = callerMethod.ContainingType?.Name ?? string.Empty,
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Depth = depth
                });
            }
        }
    }

    private static async Task FindCallees(
        IMethodSymbol method,
        Solution solution,
        List<CallHierarchyEntry> results,
        int depth,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        if (depth >= maxDepth)
            return;

        foreach (var syntaxRef in method.DeclaringSyntaxReferences)
        {
            var syntax = await syntaxRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            if (syntax is not MethodDeclarationSyntax methodDecl)
                continue;

            var document = solution.GetDocument(syntax.SyntaxTree);
            if (document is null)
                continue;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                continue;

            var invocations = methodDecl.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var invokedSymbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                if (invokedSymbol is IMethodSymbol invokedMethod)
                {
                    var lineSpan = invocation.GetLocation().GetLineSpan();

                    results.Add(new CallHierarchyEntry
                    {
                        Name = invokedMethod.Name,
                        FullName = invokedMethod.ToDisplayString(),
                        ContainingType = invokedMethod.ContainingType?.Name ?? string.Empty,
                        FilePath = lineSpan.Path ?? string.Empty,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Depth = depth
                    });
                }
            }
        }
    }
}
