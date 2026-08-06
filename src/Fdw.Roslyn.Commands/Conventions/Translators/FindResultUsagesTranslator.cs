using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Commands;
using Fdw.Roslyn.Commands.Conventions.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// Translator for finding IGenericResult usage patterns.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindResultUsages")]
public sealed class FindResultUsagesTranslator
    : RoslynCommandTranslatorBase<FindResultUsagesCommand, QueryResult<ResultUsagesData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindResultUsagesTranslator"/> class.
    /// </summary>
    public FindResultUsagesTranslator()
        : base("FindResultUsagesTranslator", "Translates Result usages search commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: find methods returning IGenericResult, collect usage data
    public override async Task<IGenericResult<QueryResult<ResultUsagesData>>> Translate(
        FindResultUsagesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var resultUsages = new List<ResultUsageInfo>();

        foreach (var project in solution.Projects)
        {
            if (!string.IsNullOrEmpty(command.ProjectFilter) &&
                !project.Name.Contains(command.ProjectFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            foreach (var document in project.Documents)
            {
                if (command.IsGeneratedDocument(document)) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null) continue;

                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);

                // Find methods returning IGenericResult
                var methodDeclarations = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var methodDecl in methodDeclarations)
                {
                    var returnType = methodDecl.ReturnType.ToString();
                    if (!returnType.Contains("IGenericResult", StringComparison.Ordinal) &&
                        !returnType.Contains("GenericResult", StringComparison.Ordinal))
                        continue;

                    if (semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken) is not IMethodSymbol methodSymbol)
                        continue;

                    var containingType = methodSymbol.ContainingType?.Name ?? "Unknown";

                    // Analyze return statements
                    var returnStatements = methodDecl.DescendantNodes().OfType<ReturnStatementSyntax>().ToList();
                    var successCount = returnStatements.Count(r =>
                        r.Expression?.ToString().Contains("Success", StringComparison.Ordinal) == true);
                    var failureCount = returnStatements.Count(r =>
                        r.Expression?.ToString().Contains("Failure", StringComparison.Ordinal) == true);

                    resultUsages.Add(new ResultUsageInfo
                    {
                        MethodName = methodSymbol.Name,
                        ContainingType = containingType,
                        ReturnType = returnType,
                        Project = project.Name,
                        FilePath = document.FilePath ?? document.Name,
                        Line = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        SuccessReturns = successCount,
                        FailureReturns = failureCount,
                        TotalReturns = returnStatements.Count
                    });
                }
            }
        }

        var data = new ResultUsagesData
        {
            Count = resultUsages.Count,
            ProjectFilter = command.ProjectFilter ?? "(all)",
            Usages = resultUsages
        };

        var result = new QueryResult<ResultUsagesData>(
            $"Found {resultUsages.Count} methods returning IGenericResult",
            data);

        return GenericResult<QueryResult<ResultUsagesData>>.Success(result);
    }
#pragma warning restore MA0051
}
