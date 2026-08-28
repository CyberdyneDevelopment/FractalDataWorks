using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// Asks the compiler whether a move actually collides, rather than guessing from names or paths.
/// </summary>
/// <remarks>
/// A filename check answers "is that path taken", which is not the question. A genuine collision happens
/// whenever the target already binds the name — a type declared in a differently-named file, a partial, a
/// generated source, or the nastier case where a NAMESPACE SEGMENT is also a type name, so
/// <c>A.B.C</c> stops resolving because <c>B</c> binds to a type instead of a namespace. None of those
/// are visible to name matching; all of them are visible to the compiler.
/// </remarks>
public static class TypeCollisionProbe
{
    /// <summary>
    /// The diagnostics that mean "this name no longer binds to one thing".
    /// </summary>
    /// <remarks>
    /// CS0101/CS0102 duplicate definitions; CS0111 duplicate member; CS0118 a name used as the wrong kind
    /// (the namespace-segment-is-a-type case); CS0426 a type name looked up inside a type; CS0104 an
    /// ambiguous reference between two imported names.
    /// </remarks>
    private static readonly string[] CollisionDiagnostics =
    {
        "CS0101", "CS0102", "CS0111", "CS0118", "CS0426", "CS0104",
    };

    /// <summary>
    /// The diagnostics that mean "this name no longer binds to anything".
    /// </summary>
    /// <remarks>
    /// The other half of what a rewrite can break. A collision is two things claiming one name; these are
    /// references left pointing at a name that moved out from under them — the references a namespace
    /// rewrite failed to follow.
    /// </remarks>
    private static readonly string[] UnresolvedDiagnostics =
    {
        "CS0246", "CS0234", "CS0103", "CS0122",
    };

    /// <summary>
    /// Compiles the affected projects and reports every collision the move would cause.
    /// </summary>
    /// <param name="solution">The solution AFTER the move has been applied in memory.</param>
    /// <param name="projectIds">The projects to probe — normally the move's source and target.</param>
    /// <param name="command">The command whose generated-file policy filters the diagnostics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One finding per collision, empty when the move binds cleanly.</returns>
    public static Task<IReadOnlyList<BreakFinding>> Probe(
        Solution solution,
        IReadOnlyList<ProjectId> projectIds,
        RoslynCommandBase command,
        CancellationToken cancellationToken = default) =>
        Probe(solution, projectIds, Array.Empty<string>(), command, baseline: null, cancellationToken);

    /// <summary>
    /// Compiles the affected projects and attributes each finding to the moved type it belongs to.
    /// </summary>
    /// <param name="solution">The solution AFTER the change has been applied in memory.</param>
    /// <param name="projectIds">The projects to probe.</param>
    /// <param name="movedTypeNames">The type names being moved, used to attribute each finding.</param>
    /// <param name="command">The command whose generated-file policy filters the diagnostics.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>One finding per problem, attributed where possible.</returns>
    /// <param name="baseline">
    /// Errors the affected projects already had BEFORE the change, from
    /// <see cref="DiagnosticDiff.Counts(Project, CancellationToken)"/>. Occurrences accounted for here are
    /// not reported. Pass <see langword="null"/> to report absolute errors.
    /// </param>
    /// <remarks>
    /// Without a baseline this reports every error in the affected projects, not the ones the change
    /// caused — so any solution that already had an error saw the move refused for breaks it did not
    /// introduce, which is most real solutions mid-refactor. The multiset semantics matter: a baseline of
    /// two occurrences and an after-count of three means ONE new break, not zero.
    /// </remarks>
    public static async Task<IReadOnlyList<BreakFinding>> Probe(
        Solution solution,
        IReadOnlyList<ProjectId> projectIds,
        IReadOnlyList<string> movedTypeNames,
        RoslynCommandBase command,
        IReadOnlyDictionary<string, int>? baseline,
        CancellationToken cancellationToken = default)
    {
        if (solution is null) throw new ArgumentNullException(nameof(solution));
        if (projectIds is null) throw new ArgumentNullException(nameof(projectIds));
        if (command is null) throw new ArgumentNullException(nameof(command));

        var findings = new List<BreakFinding>();

        foreach (var projectId in projectIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var project = solution.GetProject(projectId);
            if (project is null) continue;

            findings.AddRange(await ProbeProject(project, movedTypeNames, command, baseline, cancellationToken).ConfigureAwait(false));
        }

        return findings;
    }

    private static string Attribute(IReadOnlyList<string> movedTypeNames, string path, string message)
    {
        if (movedTypeNames.Count == 0) return string.Empty;

        var fileStem = System.IO.Path.GetFileNameWithoutExtension(path);
        foreach (var name in movedTypeNames)
        {
            if (string.Equals(name, fileStem, StringComparison.Ordinal)) return name;
        }

        foreach (var name in movedTypeNames)
        {
            if (message.Contains(name, StringComparison.Ordinal)) return name;
        }

        return string.Empty;
    }

    private static string? Classify(string diagnosticId)
    {
        if (Array.Exists(CollisionDiagnostics, id => string.Equals(id, diagnosticId, StringComparison.Ordinal)))
            return "TypeCollision";
        if (Array.Exists(UnresolvedDiagnostics, id => string.Equals(id, diagnosticId, StringComparison.Ordinal)))
            return "UnresolvedReference";

        return null;
    }

    private static async Task<IReadOnlyList<BreakFinding>> ProbeProject(
        Project project,
        IReadOnlyList<string> movedTypeNames,
        RoslynCommandBase command,
        IReadOnlyDictionary<string, int>? baseline,
        CancellationToken cancellationToken)
    {
        var findings = new List<BreakFinding>();

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null) return findings;

        if (compilation.GetSpecialType(SpecialType.System_Object).TypeKind == TypeKind.Error)
        {
            findings.Add(new BreakFinding
            {
                Kind = "ProbeUnavailable",
                FilePath = project.FilePath ?? string.Empty,
                Severity = "High",
                Detail =
                    $"Cannot verify '{project.Name}': its compilation has no framework references, so every " +
                    "name fails to bind and no collision or unresolved-reference finding from it would be " +
                    "meaningful. Usually the project did not restore or MSBuild could not evaluate it — " +
                    "restore/build the solution and reload. Findings for this project are NOT reported.",
            });

            return findings;
        }

        foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
        {
            if (diagnostic.Severity != DiagnosticSeverity.Error) continue;
            var kind = Classify(diagnostic.Id);
            if (kind is null) continue;

            var span = diagnostic.Location.GetLineSpan();

            if (command.IsGeneratedPath(span.Path)) continue;

            var message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture);

            if (baseline is not null)
            {
                // Same key shape as DiagnosticDiff.Counts, deliberately without source offsets: adding or
                // removing a using shifts every line below it, and keying on position would report the
                // whole rest of the file as newly broken.
                var key = string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"{diagnostic.Id}|{span.Path}|{message}");

                seen[key] = seen.TryGetValue(key, out var already) ? already + 1 : 1;
                if (seen[key] <= (baseline.TryGetValue(key, out var had) ? had : 0)) continue;
            }

            findings.Add(new BreakFinding
            {
                Kind = kind,
                AffectedType = Attribute(movedTypeNames, span.Path, message),
                FilePath = span.Path,
                Severity = "High",
                Detail =
                    $"{diagnostic.Id} in '{project.Name}' at line {span.StartLinePosition.Line + 1}: {message}",
            });
        }

        return findings;
    }
}
