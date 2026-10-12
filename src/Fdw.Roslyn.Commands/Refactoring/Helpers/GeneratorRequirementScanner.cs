using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Fdw.Roslyn.Commands.Refactoring.Results;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// Finds the source generators moved code needs in order to be COMPLETE, not merely to bind.
/// </summary>
/// <remarks>
/// A symbol-graph closure answers "what does this code USE" — it cannot answer "what does this code need
/// in order to exist". Code carrying [MessageLogging], [ManagedConfiguration] or [TypeOption] has members
/// that only appear when a generator runs, so moving it to a project without the generator referenced
/// produces CS8795 (partial method with no implementation) — or worse, silently missing registrations
/// that fail at runtime rather than compile time.
///
/// The generator is referenced as an ANALYZER (OutputItemType="Analyzer" ReferenceOutputAssembly="false"),
/// so it never appears as a metadata reference and no amount of symbol scanning will find it. It has to
/// be derived from the attributes present in the moved source.
/// </remarks>
public static class GeneratorRequirementScanner
{
    /// <summary>
    /// Maps a marker attribute to the generator project that completes it.
    /// </summary>
    /// <remarks>
    /// Matched on the attribute's simple name without its "Attribute" suffix, because source uses the
    /// short form. Kept as data rather than logic so a new generator is one line.
    /// </remarks>
    private static readonly (string Attribute, string GeneratorProject)[] Generators =
    {
        ("MessageLogging", "Fdw.MessageLogging.SourceGenerators"),
        ("MessageLoggingTypeCode", "Fdw.MessageLogging.SourceGenerators"),
        ("TypeOption", "Fdw.Collections.SourceGenerators"),
        ("TypeCollection", "Fdw.Collections.SourceGenerators"),
        ("ManagedConfiguration", "Fdw.Configuration.SourceGenerators"),
        ("GenerateMapper", "Fdw.Data.SourceGenerators"),
    };

    /// <summary>
    /// Determines which generator projects the given documents require.
    /// </summary>
    /// <param name="documents">The documents being moved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Generator project names, with the attribute that demanded each.</returns>
    public static async Task<IReadOnlyList<GeneratorRequirement>> Scan(
        IReadOnlyList<Document> documents,
        CancellationToken cancellationToken = default)
    {
        if (documents is null) throw new ArgumentNullException(nameof(documents));

        var found = new Dictionary<string, GeneratorRequirement>(StringComparer.Ordinal);

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) continue;

            foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
            {
                var name = Simplify(attribute.Name.ToString());

                foreach (var (marker, project) in Generators)
                {
                    if (!string.Equals(name, marker, StringComparison.Ordinal)) continue;
                    if (found.ContainsKey(project)) continue;

                    found[project] = new GeneratorRequirement
                    {
                        GeneratorProject = project,
                        BecauseOf = $"[{marker}] in {document.Name}",
                    };
                }
            }
        }

        return found.Values.ToList();
    }

    /// <summary>
    /// Reduces an attribute name to its bare marker form.
    /// </summary>
    /// <param name="written">The name as written, possibly qualified and possibly suffixed.</param>
    /// <returns>The simple name without namespace or "Attribute" suffix.</returns>
    private static string Simplify(string written)
    {
        var lastDot = written.LastIndexOf('.');
        var simple = lastDot >= 0 ? written[(lastDot + 1)..] : written;

        return simple.EndsWith("Attribute", StringComparison.Ordinal)
            ? simple[..^"Attribute".Length]
            : simple;
    }
}
