using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Search.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Resolves a type-name input (fully qualified or simple) to an
/// <see cref="INamedTypeSymbol"/> by walking the solution's compilations.
/// </summary>
internal static class FamilyTypeResolver
{
    /// <summary>
    /// Resolves a type by name. Tries fully-qualified metadata-name first
    /// against every compilation, then falls back to a simple-name search.
    /// </summary>
    /// <param name="typeName">Fully qualified or simple type name.</param>
    /// <param name="solution">The solution to search.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger.Instance"/> when not supplied.</param>
    public static async Task<INamedTypeSymbol?> Resolve(
        string typeName,
        Solution solution,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;
        FamilyTypeResolverLog.ResolveStart(logger, typeName);

        // 1) fully-qualified attempt
        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                FamilyTypeResolverLog.Cancelled(logger, typeName);
                return null;
            }

            FamilyTypeResolverLog.FqnAttempt(logger, project.Name, typeName);

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                continue;

            var direct = compilation.GetTypeByMetadataName(typeName);
            if (direct is not null)
            {
                FamilyTypeResolverLog.FqnHit(logger, project.Name, typeName);
                return direct;
            }
        }

        // 2) simple-name fallback
        var simpleName = typeName.Contains('.') ? typeName.Substring(typeName.LastIndexOf('.') + 1) : typeName;
        FamilyTypeResolverLog.SimpleNameFallback(logger, simpleName);

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                FamilyTypeResolverLog.Cancelled(logger, typeName);
                return null;
            }

            FamilyTypeResolverLog.SimpleNameAttempt(logger, project.Name, simpleName);

            var declarations = await SymbolFinder.FindDeclarationsAsync(
                project, simpleName, ignoreCase: false, SymbolFilter.Type, cancellationToken).ConfigureAwait(false);

            var match = declarations.OfType<INamedTypeSymbol>().FirstOrDefault();
            if (match is not null)
            {
                FamilyTypeResolverLog.SimpleNameHit(logger, project.Name, match.ToDisplayString());
                return match;
            }
        }

        FamilyTypeResolverLog.NotFound(logger, typeName);
        return null;
    }
}
