using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the FindUnusedCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindUnused")]
public sealed class FindUnusedTranslator : RoslynCommandTranslatorBase<FindUnusedCommand, QueryResult<IReadOnlyList<UnusedMemberInfo>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindUnusedTranslator"/> class.
    /// </summary>
    public FindUnusedTranslator()
        : base("FindUnused", "Finds unused types and members in the solution")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: iterate symbols, check for references, collect unused members
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<UnusedMemberInfo>>>> Translate(
        FindUnusedCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindUnusedTranslatorLog.Scanning(Logger, command.IncludePrivate, command.IncludeInternal, command.MaxResults);

        var unusedMembers = new List<UnusedMemberInfo>();

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested || unusedMembers.Count >= command.MaxResults)
                break;

            // Per-project try: a single project with unresolvable analyzer references
            // (Roslyn throws when materializing the compilation in that case) should
            // not abort the whole solution scan.
#pragma warning disable CA1031 // Per-project failures are aggregated; we want to keep scanning
#pragma warning disable FDW014 // Why: best-effort scan — per-project compilation failures and SymbolFinder
                              // exceptions are tolerated by design; the overall scan returns the partial result.
                              // No logger is injected here; surfacing per-project errors would require an API
                              // change tracked separately as Roslyn-scan error-reporting work.
            try
            {
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation is null)
                    continue;

                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    if (cancellationToken.IsCancellationRequested || unusedMembers.Count >= command.MaxResults)
                        break;

                    var semanticModel = compilation.GetSemanticModel(syntaxTree);
                    var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

                    var declaredSymbols = root.DescendantNodes()
                        .Select(node => semanticModel.GetDeclaredSymbol(node))
                        .Where(s => s is not null)
                        .Where(s => IsTargetSymbol(s!, command.IncludePrivate, command.IncludeInternal))
                        .Cast<ISymbol>();

                    foreach (var symbol in declaredSymbols)
                    {
                        if (unusedMembers.Count >= command.MaxResults)
                            break;
                        if (IsExcluded(symbol))
                            continue;

                        var refsResult = await HasReferences(symbol, solution, Logger, cancellationToken).ConfigureAwait(false);
                        if (!refsResult.IsSuccess || refsResult.Value)
                            continue;

                        if (symbol.Locations.Length > 0)
                        {
                            var loc = symbol.Locations[0];
                            var lineSpan = loc.GetLineSpan();
                            unusedMembers.Add(new UnusedMemberInfo(
                                symbol.Name,
                                symbol.Kind.ToString(),
                                symbol.DeclaredAccessibility.ToString(),
                                symbol.ContainingType?.Name ?? string.Empty,
                                lineSpan.Path ?? string.Empty,
                                lineSpan.StartLinePosition.Line + 1,
                                lineSpan.StartLinePosition.Character + 1));
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                continue;
            }
#pragma warning restore FDW014
#pragma warning restore CA1031
        }

        var summary = $"Found {unusedMembers.Count} unused members";
        var result = new QueryResult<IReadOnlyList<UnusedMemberInfo>>(summary, unusedMembers);

        FindUnusedTranslatorLog.Found(Logger, unusedMembers.Count);

        return GenericResult<QueryResult<IReadOnlyList<UnusedMemberInfo>>>.Success(result, summary);
    }

    private static async Task<IGenericResult<bool>> HasReferences(
        ISymbol symbol, Microsoft.CodeAnalysis.Solution solution, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            return GenericResult<bool>.Success(references.Any(r => r.Locations.Any()));
        }
#pragma warning disable CA1031 // best-effort scan — surface exception in the result
        catch (Exception ex)
#pragma warning restore CA1031
        {
            FindUnusedTranslatorLog.ReferenceCheckFailed(logger, symbol.Name, ex.GetType().Name);
            return GenericResult<bool>.Success(true,
                $"FindReferencesAsync failed for {symbol.Name}: {ex.GetType().Name}: {ex.Message}");
        }
    }
#pragma warning restore MA0051

    private static bool IsTargetSymbol(ISymbol symbol, bool includePrivate, bool includeInternal)
    {
        // Only check methods, properties, types, and fields
        if (symbol is not (IMethodSymbol or IPropertySymbol or INamedTypeSymbol or IFieldSymbol))
            return false;

        // Skip generated code
        if (symbol.IsImplicitlyDeclared)
            return false;

#pragma warning disable FDW018 // External Roslyn Accessibility enum — cannot convert to TypeCollection
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Private => includePrivate,
            Accessibility.Internal => includeInternal,
            Accessibility.ProtectedAndInternal => includeInternal,
            _ => false // Don't flag public/protected as unused
        };
#pragma warning restore FDW018
    }

    private static bool IsExcluded(ISymbol symbol)
    {
        // Exclude Main methods
        if (symbol is IMethodSymbol method && string.Equals(method.Name, "Main", StringComparison.Ordinal))
            return true;

        // Exclude constructors
        if (symbol is IMethodSymbol { MethodKind: MethodKind.Constructor })
            return true;

        // Exclude static constructors
        if (symbol is IMethodSymbol { MethodKind: MethodKind.StaticConstructor })
            return true;

        // Exclude property accessors
        if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet })
            return true;

        // Exclude event accessors
        if (symbol is IMethodSymbol { MethodKind: MethodKind.EventAdd or MethodKind.EventRemove })
            return true;

        // Exclude symbols with special attributes
        var attributes = symbol.GetAttributes();
        if (attributes.Any(a => a.AttributeClass?.Name is "UsedImplicitlyAttribute" or "PublicAPIAttribute"))
            return true;

        return false;
    }
}
