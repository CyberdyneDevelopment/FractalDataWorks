using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// Resolves every symbol a document references to the assembly that declares it.
/// </summary>
/// <remarks>
/// This is what turns "which references does the target need" from a judgement call into a computation.
/// Working it out by eye is the step that gets a cross-project move wrong.
/// </remarks>
public static class AssemblyUsageScanner
{
    /// <summary>
    /// Scans a document and returns, per referenced assembly, how many distinct symbols resolved to it
    /// and one representative type name.
    /// </summary>
    /// <param name="document">The document to scan.</param>
    /// <param name="ownAssembly">The assembly the document currently belongs to; excluded from results.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A map of assembly name to usage.</returns>
    public static async Task<IReadOnlyDictionary<string, AssemblyUsage>> Scan(
        Document document,
        string? ownAssembly,
        CancellationToken cancellationToken = default)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));

        var usage = new Dictionary<string, AssemblyUsage>(StringComparer.Ordinal);

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null || root is null) return usage;

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            Record(semanticModel.GetSymbolInfo(node, cancellationToken).Symbol, ownAssembly, usage, seen);
            Record(semanticModel.GetTypeInfo(node, cancellationToken).Type, ownAssembly, usage, seen);
        }

        return usage;
    }

    private static void Record(
        ISymbol? symbol,
        string? ownAssembly,
        Dictionary<string, AssemblyUsage> usage,
        HashSet<string> seen)
    {
        var assembly = symbol?.ContainingAssembly?.Name;
        if (string.IsNullOrEmpty(assembly)) return;
        if (IsFrameworkAssembly(assembly!)) return;
        if (string.Equals(assembly, ownAssembly, StringComparison.Ordinal)) return;

        var key = assembly + "|" + symbol!.ToDisplayString();
        if (!seen.Add(key)) return;

        if (usage.TryGetValue(assembly!, out var existing))
        {
            existing.SymbolCount++;
            return;
        }

        usage[assembly!] = new AssemblyUsage
        {
            Assembly = assembly!,
            SymbolCount = 1,
            ExampleSymbol = symbol.ToDisplayString(),
        };
    }

    private static bool IsFrameworkAssembly(string assembly) =>
        assembly.StartsWith("System", StringComparison.Ordinal)
        || assembly.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
        || assembly.StartsWith("netstandard", StringComparison.Ordinal)
        || assembly.StartsWith("mscorlib", StringComparison.Ordinal);
}
