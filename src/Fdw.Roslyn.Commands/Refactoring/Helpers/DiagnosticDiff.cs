using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// Compares a solution's compiler errors before and after a change.
/// </summary>
/// <remarks>
/// The compiler is the oracle. Rather than reimplement C# name resolution to work out what a refactor
/// broke, apply it and ask what changed: the errors that APPEAR are the work to repair, and the ones
/// that DISAPPEAR are problems the change fixed. Reimplementing lookup would have to model extension
/// methods, aliases, nested-namespace scope and same-named types across imports, and would be wrong in
/// a way nothing checks.
///
/// Extracted from RemoveGlobalUsingsTranslator, which proved the approach. It was private there, so the
/// move commands instead reported the compilation's ABSOLUTE errors with no baseline — meaning any
/// solution that already had an error, which is most real ones mid-refactor, saw every move refused for
/// breaks it did not cause. Sharing the mechanism is the fix; a second copy would drift.
/// </remarks>
public static class DiagnosticDiff
{
    /// <summary>
    /// Counts each distinct compiler error across the given projects.
    /// </summary>
    /// <param name="projects">The projects to compile.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// A multiset keyed by error identity, or <see langword="null"/> if any project's compilation cannot
    /// bind — in which case no diff from it would mean anything.
    /// </returns>
    /// <remarks>
    /// A multiset rather than a set because the same error can legitimately occur more than once in a
    /// file, and a change that adds a second occurrence has broken something new.
    ///
    /// The key deliberately excludes source offsets. Line numbers shift when a using is added or
    /// removed, so keying on position would report every diagnostic below the edit as "new".
    /// </remarks>
    public static async Task<IReadOnlyDictionary<string, int>?> Counts(
        IEnumerable<Project> projects,
        CancellationToken cancellationToken = default)
    {
        if (projects is null) throw new ArgumentNullException(nameof(projects));

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) return null;

            if (compilation.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error) return null;

            foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error) continue;

                var key = string.Create(CultureInfo.InvariantCulture,
                    $"{diagnostic.Id}|{diagnostic.Location.GetLineSpan().Path}|{diagnostic.GetMessage(CultureInfo.InvariantCulture)}");

                counts[key] = counts.TryGetValue(key, out var existing) ? existing + 1 : 1;
            }
        }

        return counts;
    }

    /// <summary>
    /// Counts the errors for a single project.
    /// </summary>
    /// <param name="project">The project to compile.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The multiset, or <see langword="null"/> if the compilation cannot bind.</returns>
    public static Task<IReadOnlyDictionary<string, int>?> Counts(
        Project project,
        CancellationToken cancellationToken = default)
    {
        if (project is null) throw new ArgumentNullException(nameof(project));

        return Counts(new[] { project }, cancellationToken);
    }

    /// <summary>
    /// Returns the errors present in <paramref name="after"/> beyond what <paramref name="before"/> held.
    /// </summary>
    /// <param name="before">The baseline multiset.</param>
    /// <param name="after">The multiset taken after the change.</param>
    /// <returns>One entry per newly-appearing occurrence.</returns>
    /// <remarks>
    /// Swap the arguments to get what the change RESOLVED — the operation is the same, and a refactor
    /// that fixes existing errors is worth reporting rather than hiding.
    /// </remarks>
    public static IReadOnlyList<string> Appeared(
        IReadOnlyDictionary<string, int> before,
        IReadOnlyDictionary<string, int> after)
    {
        if (before is null) throw new ArgumentNullException(nameof(before));
        if (after is null) throw new ArgumentNullException(nameof(after));

        var appeared = new List<string>();
        foreach (var pair in after)
        {
            var had = before.TryGetValue(pair.Key, out var count) ? count : 0;
            for (var i = 0; i < pair.Value - had; i++) appeared.Add(pair.Key);
        }

        return appeared;
    }

    /// <summary>Gets the file path encoded in a diagnostic key.</summary>
    /// <param name="diagnosticKey">A key produced by <see cref="Counts(Project, CancellationToken)"/>.</param>
    /// <returns>The path, or empty when the key carries none.</returns>
    public static string PathOf(string diagnosticKey)
    {
        if (diagnosticKey is null) throw new ArgumentNullException(nameof(diagnosticKey));

        var parts = diagnosticKey.Split('|');
        return parts.Length > 1 ? parts[1] : string.Empty;
    }

    /// <summary>Gets the error id encoded in a diagnostic key.</summary>
    /// <param name="diagnosticKey">A key produced by <see cref="Counts(Project, CancellationToken)"/>.</param>
    /// <returns>The id, for example <c>CS0234</c>.</returns>
    public static string IdOf(string diagnosticKey)
    {
        if (diagnosticKey is null) throw new ArgumentNullException(nameof(diagnosticKey));

        var parts = diagnosticKey.Split('|');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    /// <summary>Gets the message encoded in a diagnostic key.</summary>
    /// <param name="diagnosticKey">A key produced by <see cref="Counts(Project, CancellationToken)"/>.</param>
    /// <returns>The compiler's message text.</returns>
    public static string MessageOf(string diagnosticKey)
    {
        if (diagnosticKey is null) throw new ArgumentNullException(nameof(diagnosticKey));

        var parts = diagnosticKey.Split('|');
        return parts.Length > 2 ? string.Join("|", parts.Skip(2)) : string.Empty;
    }
}
