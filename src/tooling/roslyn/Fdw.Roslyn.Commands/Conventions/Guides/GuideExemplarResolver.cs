using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Conventions.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Locates a guide's exemplar in the loaded solution through the file the guide cites.
/// </summary>
/// <remarks>
/// The cited file is looked up by path suffix, so no compilation is built to find it. A guide stamped in one
/// repository still resolves when the solution holds that repository at a different location.
/// </remarks>
internal static class GuideExemplarResolver
{
    /// <summary>
    /// Resolves one exemplar.
    /// </summary>
    /// <param name="solution">The loaded solution.</param>
    /// <param name="exemplar">The exemplar a guide names.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exemplar with its live location and summary, or the reason it could not be located.</returns>
    public static async Task<GuideExemplarInfo> Resolve(
        Solution solution,
        GuideExemplarModel exemplar,
        CancellationToken cancellationToken = default)
    {
        var document = FindDocument(solution, exemplar.Cite);
        if (document is null)
            return Describe(exemplar, GuideExemplarResolution.FileNotInSolution, null, null, null);

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || model is null)
            return Describe(exemplar, GuideExemplarResolution.TypeNotInFile, document.FilePath, null, null);

        foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol symbol) continue;
            if (!string.Equals(GuideTypeResolver.FullName(symbol), exemplar.Type, StringComparison.Ordinal)) continue;

            var line = declaration.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            return Describe(exemplar, GuideExemplarResolution.Resolved, document.FilePath, line, GuideDocumentation.Summary(symbol));
        }

        return Describe(exemplar, GuideExemplarResolution.TypeNotInFile, document.FilePath, null, null);
    }

    private static Document? FindDocument(Solution solution, string cite)
    {
        var colon = cite.LastIndexOf(':');
        var path = (colon < 0 ? cite : cite[..colon]).Replace('\\', '/');
        var firstSlash = path.IndexOf('/', StringComparison.Ordinal);
        var suffix = "/" + (firstSlash < 0 ? path : path[(firstSlash + 1)..]);

        return solution.Projects
            .SelectMany(p => p.Documents)
            .FirstOrDefault(d => d.FilePath is not null
                && d.FilePath.Replace('\\', '/').EndsWith(suffix, StringComparison.Ordinal));
    }

    private static GuideExemplarInfo Describe(
        GuideExemplarModel exemplar,
        GuideExemplarResolution resolution,
        string? filePath,
        int? line,
        string? summary) =>
        new()
        {
            Type = exemplar.Type,
            Role = exemplar.Role,
            StampedCite = exemplar.Cite,
            Resolution = resolution,
            FilePath = filePath,
            Line = line,
            Summary = summary
        };
}
