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
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// Translator for analyzing exception usage patterns.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AnalyzeExceptionUsage")]
public sealed class AnalyzeExceptionUsageTranslator
    : RoslynCommandTranslatorBase<AnalyzeExceptionUsageCommand, QueryResult<ExceptionUsageAnalysisData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeExceptionUsageTranslator"/> class.
    /// </summary>
    public AnalyzeExceptionUsageTranslator()
        : base("AnalyzeExceptionUsageTranslator", "Translates exception usage analysis commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: find throw statements and try-catch blocks, build analysis
    public override async Task<IGenericResult<QueryResult<ExceptionUsageAnalysisData>>> Translate(
        AnalyzeExceptionUsageCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        AnalyzeExceptionUsageTranslatorLog.Analyzing(Logger, command.ProjectFilter ?? "(all)");

        var throwStatements = new List<ThrowStatementInfo>();
        var tryCatchBlocks = new List<TryCatchBlockInfo>();

        foreach (var project in solution.Projects)
        {
            if (!string.IsNullOrEmpty(command.ProjectFilter) &&
                !project.Name.Contains(command.ProjectFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null) continue;

                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);

                // Analyze throw statements
                var throws = syntaxRoot.DescendantNodes().OfType<ThrowStatementSyntax>();
                foreach (var throwStmt in throws)
                {
                    var containingMethod = throwStmt.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    var methodName = containingMethod?.Identifier.Text ?? "(unknown)";
                    var containingType = containingMethod?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "(unknown)";

                    var exceptionType = "Unknown";
                    if (throwStmt.Expression is ObjectCreationExpressionSyntax creation)
                    {
                        exceptionType = creation.Type.ToString();
                    }

                    var category = CategorizeException(exceptionType);

                    throwStatements.Add(new ThrowStatementInfo
                    {
                        ExceptionType = exceptionType,
                        Category = category,
                        MethodName = methodName,
                        ContainingType = containingType,
                        Project = project.Name,
                        FilePath = document.FilePath ?? document.Name,
                        Line = throwStmt.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        IsResultCandidate = string.Equals(category, "BusinessLogic", StringComparison.Ordinal)
                    });
                }

                // Analyze try-catch blocks
                var tryCatches = syntaxRoot.DescendantNodes().OfType<TryStatementSyntax>();
                foreach (var tryCatch in tryCatches)
                {
                    var containingMethod = tryCatch.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    var methodName = containingMethod?.Identifier.Text ?? "(unknown)";
                    var containingType = containingMethod?.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text ?? "(unknown)";

                    var catchTypes = tryCatch.Catches
                        .Select(c => c.Declaration?.Type?.ToString() ?? "Exception")
                        .ToList();

                    tryCatchBlocks.Add(new TryCatchBlockInfo
                    {
                        CatchTypes = catchTypes,
                        CatchCount = tryCatch.Catches.Count,
                        HasFinally = tryCatch.Finally is not null,
                        MethodName = methodName,
                        ContainingType = containingType,
                        Project = project.Name,
                        FilePath = document.FilePath ?? document.Name,
                        Line = tryCatch.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
        }

        var resultCandidates = throwStatements.Count(t => t.IsResultCandidate);

        var data = new ExceptionUsageAnalysisData
        {
            ThrowCount = throwStatements.Count,
            TryCatchCount = tryCatchBlocks.Count,
            ResultPatternCandidates = resultCandidates,
            ProjectFilter = command.ProjectFilter ?? "(all)",
            ThrowStatements = throwStatements,
            TryCatchBlocks = tryCatchBlocks
        };

        var result = new QueryResult<ExceptionUsageAnalysisData>(
            $"Found {throwStatements.Count} throw statements and {tryCatchBlocks.Count} try-catch blocks",
            data);

        AnalyzeExceptionUsageTranslatorLog.Analyzed(Logger, throwStatements.Count, tryCatchBlocks.Count);

        return GenericResult<QueryResult<ExceptionUsageAnalysisData>>.Success(result);
    }
#pragma warning restore MA0051

    private static string CategorizeException(string exceptionType)
    {
        if (exceptionType.Contains("ArgumentNull", StringComparison.Ordinal) ||
            exceptionType.Contains("ArgumentOutOfRange", StringComparison.Ordinal) ||
            exceptionType.Contains("ArgumentException", StringComparison.Ordinal))
            return "ArgumentValidation";

        if (exceptionType.Contains("InvalidOperation", StringComparison.Ordinal) ||
            exceptionType.Contains("NotSupported", StringComparison.Ordinal) ||
            exceptionType.Contains("NotImplemented", StringComparison.Ordinal))
            return "StateValidation";

        if (exceptionType.Contains("IO", StringComparison.Ordinal) ||
            exceptionType.Contains("File", StringComparison.Ordinal) ||
            exceptionType.Contains("Directory", StringComparison.Ordinal) ||
            exceptionType.Contains("Socket", StringComparison.Ordinal) ||
            exceptionType.Contains("Http", StringComparison.Ordinal))
            return "Infrastructure";

        if (exceptionType.Contains("Timeout", StringComparison.Ordinal) ||
            exceptionType.Contains("Cancelled", StringComparison.Ordinal) ||
            exceptionType.Contains("Canceled", StringComparison.Ordinal))
            return "Cancellation";

        return "BusinessLogic";
    }
}
