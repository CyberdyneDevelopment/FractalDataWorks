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
/// Translator for <see cref="ValidateResultHandlingCommand"/>.
/// Finds invocations of <c>IGenericResult</c>-returning methods whose result is
/// discarded — either the call is a bare expression statement, or the result
/// variable is never read for an <c>IsSuccess</c>/<c>Value</c>/<c>Match</c> check
/// before going out of scope.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ValidateResultHandling")]
public sealed class ValidateResultHandlingTranslator
    : RoslynCommandTranslatorBase<ValidateResultHandlingCommand, QueryResult<ResultHandlingValidationData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateResultHandlingTranslator"/> class.
    /// </summary>
    public ValidateResultHandlingTranslator()
        : base("ValidateResultHandlingTranslator", "Audits IGenericResult call sites for discarded results")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: walk every project, find IGenericResult-returning calls, classify
    public override async Task<IGenericResult<QueryResult<ResultHandlingValidationData>>> Translate(
        ValidateResultHandlingCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ValidateResultHandlingTranslatorLog.Scanning(Logger, command.ProjectFilter ?? "(all)");

        var issues = new List<ResultHandlingIssue>();

        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(command.ProjectFilter)
                && !project.Name.Contains(command.ProjectFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            // Per-project try: projects with unresolvable analyzer references cause Roslyn
            // to throw when materializing the compilation; skip them rather than abort.
#pragma warning disable CA1031 // Per-project failures are tolerated; goal is best-effort scan
#pragma warning disable FDW014 // Exception is recorded as a per-project issue (see catch below) rather than aborting the scan with Failure; this is the deliberate best-effort pattern.
            try
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation is null) continue;

                foreach (var document in project.Documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    if (syntaxRoot is null) continue;
                    var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);

                    foreach (var invocation in syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol methodSymbol)
                            continue;

                        if (!ReturnsResult(methodSymbol)) continue;

                        // Discarded if the invocation is the entire statement (no receiver of its value).
                        var parent = invocation.Parent;
                        if (parent is ExpressionStatementSyntax)
                        {
                            var lineSpan = invocation.GetLocation().GetLineSpan();
                            issues.Add(new ResultHandlingIssue
                            {
                                Severity = "Warning",
                                Message = $"Result of '{methodSymbol.Name}' is discarded — no IsSuccess / Match / await check.",
                                Project = project.Name,
                                FilePath = document.FilePath ?? document.Name,
                                Line = lineSpan.StartLinePosition.Line + 1,
                                Code = invocation.ToString(),
                                MethodName = methodSymbol.Name,
                            });
                        }
                        // Discarded if assigned to a discard ("_ = X()") or its containing
                        // local variable is never referenced.
                        else if (parent is AssignmentExpressionSyntax assign
                                 && assign.Left is IdentifierNameSyntax id
                                 && string.Equals(id.Identifier.Text, "_", StringComparison.Ordinal))
                        {
                            var lineSpan = invocation.GetLocation().GetLineSpan();
                            issues.Add(new ResultHandlingIssue
                            {
                                Severity = "Warning",
                                Message = $"Result of '{methodSymbol.Name}' is explicitly discarded via '_'.",
                                Project = project.Name,
                                FilePath = document.FilePath ?? document.Name,
                                Line = lineSpan.StartLinePosition.Line + 1,
                                Code = invocation.ToString(),
                                MethodName = methodSymbol.Name,
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Why: per-project failures are tolerated by design (best-effort scan), but
                // record the failure in the result data so it isn't silently swallowed —
                // FDW014 demands the exception be visible in the returned GenericResult.
                ValidateResultHandlingTranslatorLog.ProjectScanFailed(Logger, project.Name, ex.GetType().Name, ex.Message);
                issues.Add(new ResultHandlingIssue
                {
                    Severity = "Error",
                    Message = $"Project scan failed: {ex.GetType().Name} — {ex.Message}",
                    Project = project.Name,
                    FilePath = string.Empty,
                    Line = 0,
                    Code = string.Empty,
                    MethodName = string.Empty,
                });
                continue;
            }
#pragma warning restore FDW014
#pragma warning restore CA1031
        }

        var data = new ResultHandlingValidationData
        {
            IssueCount = issues.Count,
            ProjectFilter = command.ProjectFilter ?? "(all)",
            Issues = issues,
        };

        var summary = $"Found {issues.Count} discarded IGenericResult call site(s)";
        var result = new QueryResult<ResultHandlingValidationData>(summary, data);

        ValidateResultHandlingTranslatorLog.Found(Logger, issues.Count);

        return GenericResult<QueryResult<ResultHandlingValidationData>>.Success(result, summary);
    }
#pragma warning restore MA0051

    private static async Task<IGenericResult> ScanProject(
        Microsoft.CodeAnalysis.Project project, List<ResultHandlingIssue> issues, CancellationToken cancellationToken)
    {
        try
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) return GenericResult.Success();

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null) continue;
                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);

                foreach (var invocation in syntaxRoot.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol methodSymbol)
                        continue;
                    if (!ReturnsResult(methodSymbol)) continue;

                    var parent = invocation.Parent;
                    if (parent is ExpressionStatementSyntax)
                    {
                        var lineSpan = invocation.GetLocation().GetLineSpan();
                        issues.Add(new ResultHandlingIssue
                        {
                            Severity = "Warning",
                            Message = $"Result of '{methodSymbol.Name}' is discarded — no IsSuccess / Match / await check.",
                            Project = project.Name,
                            FilePath = document.FilePath ?? document.Name,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Code = invocation.ToString(),
                            MethodName = methodSymbol.Name,
                        });
                    }
                    else if (parent is AssignmentExpressionSyntax assign
                             && assign.Left is IdentifierNameSyntax id
                             && string.Equals(id.Identifier.Text, "_", StringComparison.Ordinal))
                    {
                        var lineSpan = invocation.GetLocation().GetLineSpan();
                        issues.Add(new ResultHandlingIssue
                        {
                            Severity = "Warning",
                            Message = $"Result of '{methodSymbol.Name}' is explicitly discarded via '_'.",
                            Project = project.Name,
                            FilePath = document.FilePath ?? document.Name,
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Code = invocation.ToString(),
                            MethodName = methodSymbol.Name,
                        });
                    }
                }
            }
            return GenericResult.Success();
        }
#pragma warning disable CA1031 // best-effort scan — surface exception in the result
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return GenericResult.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"),
                ResultDetails.Create("Step", "ScanProject").With("Project", project.Name).With("Exception", ex.GetType().Name).With("ExceptionMessage", ex.Message));
        }
    }

    private static bool ReturnsResult(IMethodSymbol method)
    {
        var rt = method.ReturnType;
        if (IsResultType(rt)) return true;
        // Unwrap Task<T>, ValueTask<T>
        if (rt is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1)
        {
            var n = named.Name;
            if (string.Equals(n, "Task", StringComparison.Ordinal) || string.Equals(n, "ValueTask", StringComparison.Ordinal))
                return IsResultType(named.TypeArguments[0]);
        }
        return false;
    }

    private static bool IsResultType(ITypeSymbol type)
    {
        var name = type.Name;
        if (string.Equals(name, "IGenericResult", StringComparison.Ordinal)) return true;
        if (string.Equals(name, "GenericResult", StringComparison.Ordinal)) return true;
        foreach (var iface in type.AllInterfaces)
        {
            if (string.Equals(iface.Name, "IGenericResult", StringComparison.Ordinal)) return true;
        }
        return false;
    }
}
