using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions;
using Fdw.Services.Connections.RoslynWorkspace.Abstractions.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Stateless symbol-finding helpers — name resolution, callers, callees, implementations.
/// Operates on a <see cref="Solution"/> handed in by either the Live or Snapshot client.
/// </summary>
internal static class SymbolFinderHelper
{
    internal static async Task<IGenericResult<RoslynSymbolMatch>> ResolveSymbol(
        Solution solution,
        string name,
        string connectionName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return GenericResult<RoslynSymbolMatch>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, name, "name is null or whitespace"));

        RoslynWorkspaceConnectionLog.ResolvingSymbol(logger, connectionName, name);

        var query = name;
        string? memberPart = null;

        // Why: "Type.Member" shape — narrow to types whose name matches the head,
        //      then look up the member by name on each candidate.
        var dotIndex = name.LastIndexOf('.');
        if (dotIndex > 0 && dotIndex < name.Length - 1)
        {
            query = name.Substring(0, dotIndex);
            memberPart = name.Substring(dotIndex + 1);
        }

        var matches = await SymbolFinder.FindSourceDeclarationsAsync(
            solution, query, ignoreCase: true, SymbolFilter.TypeAndMember, cancellationToken).ConfigureAwait(false);

        ISymbol? best = null;

        if (memberPart is not null)
        {
            foreach (var typeSymbol in matches.OfType<INamedTypeSymbol>())
            {
                if (!string.Equals(typeSymbol.Name, query, StringComparison.OrdinalIgnoreCase)) continue;

                var member = typeSymbol.GetMembers()
                    .FirstOrDefault(m => string.Equals(m.Name, memberPart, StringComparison.OrdinalIgnoreCase));
                if (member is not null)
                    best = ScoreBetter(best, member, name);
            }
        }

        if (best is null)
        {
            foreach (var candidate in matches)
                best = ScoreBetter(best, candidate, name);
        }

        if (best is null)
            return GenericResult<RoslynSymbolMatch>.Failure(
                RoslynWorkspaceConnectionLog.SymbolNameUnresolved(logger, connectionName, name));

        var match = ToMatch(best);
        RoslynWorkspaceConnectionLog.SymbolResolved(logger, connectionName, name, match.DocumentationCommentId);
        return GenericResult<RoslynSymbolMatch>.Success(match);
    }

    internal static async Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallers(
        Solution solution,
        string symbolId,
        int max,
        string connectionName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbolId))
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "symbolId is null or whitespace"));

        if (max <= 0)
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "max must be > 0"));

        RoslynWorkspaceConnectionLog.FindingCallers(logger, connectionName, symbolId, max);

        try
        {
            var target = await FindSymbolById(solution, symbolId, cancellationToken).ConfigureAwait(false);
            if (target is null)
                return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                    RoslynWorkspaceConnectionLog.SymbolNotFound(logger, connectionName, symbolId));

            var callerInfo = await SymbolFinder.FindCallersAsync(target, solution, cancellationToken).ConfigureAwait(false);
            var results = callerInfo
                .Select(ci => ci.CallingSymbol)
                .Where(s => s is not null)
                .Distinct<ISymbol>(SymbolEqualityComparer.Default)
                .Take(max)
                .Select(ToMatch)
                .ToList();

            RoslynWorkspaceConnectionLog.CallersFound(logger, connectionName, symbolId, results.Count, max);
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Success(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.FindCallersFailed(logger, ex, connectionName, symbolId, ex.Message));
        }
    }

    internal static async Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindCallees(
        Solution solution,
        string symbolId,
        int max,
        string connectionName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbolId))
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "symbolId is null or whitespace"));

        if (max <= 0)
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "max must be > 0"));

        RoslynWorkspaceConnectionLog.FindingCallees(logger, connectionName, symbolId, max);

        try
        {
            var target = await FindSymbolById(solution, symbolId, cancellationToken).ConfigureAwait(false);
            if (target is null)
                return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                    RoslynWorkspaceConnectionLog.SymbolNotFound(logger, connectionName, symbolId));

            // Why: Roslyn has no built-in "find callees" — walk the symbol's declaration syntax,
            //      ask the semantic model for each invocation/member-access target, dedupe.
            var callees = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
            foreach (var declRef in target.DeclaringSyntaxReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var syntax = await declRef.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var document = solution.GetDocument(syntax.SyntaxTree);
                if (document is null) continue;
                var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (model is null) continue;

                foreach (var node in syntax.DescendantNodes())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var symbolInfo = model.GetSymbolInfo(node, cancellationToken);
                    var s = symbolInfo.Symbol;
                    if (s is null) continue;
                    if (s.Kind is SymbolKind.Method or SymbolKind.Property)
                        callees.Add(s);
                    if (callees.Count >= max) break;
                }
                if (callees.Count >= max) break;
            }

            var results = callees.Take(max).Select(ToMatch).ToList();
            RoslynWorkspaceConnectionLog.CalleesFound(logger, connectionName, symbolId, results.Count, max);
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Success(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.FindCalleesFailed(logger, ex, connectionName, symbolId, ex.Message));
        }
    }

    internal static async Task<IGenericResult<IReadOnlyList<RoslynSymbolMatch>>> FindImplementations(
        Solution solution,
        string symbolId,
        int max,
        string connectionName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbolId))
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "symbolId is null or whitespace"));

        if (max <= 0)
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.InvalidSymbolId(logger, connectionName, symbolId, "max must be > 0"));

        RoslynWorkspaceConnectionLog.FindingImplementations(logger, connectionName, symbolId, max);

        try
        {
            var target = await FindSymbolById(solution, symbolId, cancellationToken).ConfigureAwait(false);
            if (target is null)
                return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                    RoslynWorkspaceConnectionLog.SymbolNotFound(logger, connectionName, symbolId));

            var impls = await SymbolFinder.FindImplementationsAsync(target, solution, cancellationToken: cancellationToken).ConfigureAwait(false);
            var results = impls
                .Where(s => s is not null)
                .Distinct<ISymbol>(SymbolEqualityComparer.Default)
                .Take(max)
                .Select(ToMatch)
                .ToList();

            RoslynWorkspaceConnectionLog.ImplementationsFound(logger, connectionName, symbolId, results.Count, max);
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Success(results);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<RoslynSymbolMatch>>.Failure(
                RoslynWorkspaceConnectionLog.FindImplementationsFailed(logger, ex, connectionName, symbolId, ex.Message));
        }
    }

    private static async Task<ISymbol?> FindSymbolById(Solution solution, string symbolId, CancellationToken cancellationToken)
    {
        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            var candidates = DocumentationCommentId.GetSymbolsForDeclarationId(symbolId, compilation);
            if (candidates.IsDefaultOrEmpty) continue;
            return candidates[0];
        }
        return null;
    }

    private static RoslynSymbolMatch ToMatch(ISymbol symbol)
    {
        var loc = symbol.Locations.FirstOrDefault(l => l.IsInSource);
        var lineSpan = loc?.GetLineSpan();
        var id = symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString();
        var displayName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return new RoslynSymbolMatch(
            DocumentationCommentId: id,
            DisplayName: displayName,
            Kind: symbol.Kind.ToString().ToLowerInvariant(),
            FilePath: lineSpan?.Path,
            Line: lineSpan is null ? null : lineSpan.Value.StartLinePosition.Line + 1);
    }

    private static ISymbol ScoreBetter(ISymbol? current, ISymbol candidate, string query)
    {
        if (current is null) return candidate;
        return Score(candidate, query) > Score(current, query) ? candidate : current;
    }

    private static int Score(ISymbol s, string query)
    {
        var score = 0;
        if (string.Equals(s.Name, query, StringComparison.Ordinal)) score += 1000;
        else if (string.Equals(s.Name, query, StringComparison.OrdinalIgnoreCase)) score += 800;
        var path = s.Locations.FirstOrDefault(l => l.IsInSource)?.GetLineSpan().Path ?? string.Empty;
        if (path.Contains(".g.cs", StringComparison.OrdinalIgnoreCase)) score -= 200;
        if (path.Contains(".Tests", StringComparison.OrdinalIgnoreCase)) score -= 50;
        score += SymbolKindScores.ByName(s.Kind.ToString()).Weight;
        return score;
    }
}
