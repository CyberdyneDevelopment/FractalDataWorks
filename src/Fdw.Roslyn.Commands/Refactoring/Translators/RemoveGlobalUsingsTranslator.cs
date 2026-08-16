using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Helpers;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for <see cref="RemoveGlobalUsingsCommand"/>.
/// </summary>
/// <remarks>
/// The compiler is the oracle. Rather than reimplement C# name resolution to guess which files depended
/// on an import, this removes the directive and asks the compiler what changed — the diagnostics that
/// APPEAR are exactly the files to repair, and the ones that DISAPPEAR are the ambiguities the import was
/// causing. Reimplementing lookup would have to model extension methods, aliases, nested-namespace scope
/// and same-named types across imports, and would be wrong in a way nothing checks.
/// </remarks>
[TypeOption(typeof(RoslynCommandTranslators), "RemoveGlobalUsings")]
public sealed class RemoveGlobalUsingsTranslator
    : RoslynCommandTranslatorBase<RemoveGlobalUsingsCommand, IRoslynCommandResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGlobalUsingsTranslator"/> class.
    /// </summary>
    public RemoveGlobalUsingsTranslator()
        : base("RemoveGlobalUsings", "Removes global usings and gives an explicit using to every file that relied on them")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear flow: resolve, refuse duplicates, baseline, remove, diff, repair, re-probe.
    public override async Task<IGenericResult<IRoslynCommandResult>> Translate(
        RemoveGlobalUsingsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));
        if (solution is null) throw new ArgumentNullException(nameof(solution));

        RemoveGlobalUsingsTranslatorLog.Removing(Logger, command.Project ?? string.Empty, command.DryRun);

        if (string.IsNullOrWhiteSpace(command.Project))
            return Fail("ProjectNotSpecified", ResultDetails.Create());

        if (command.Namespaces.Length == 0)
            return Fail("NoGlobalUsingsMatched", ResultDetails.Create()
                .With("Namespaces", "(none requested)")
                .With("Project", command.Project));

        var matches = solution.Projects
            .Where(p => string.Equals(p.Name, command.Project, StringComparison.Ordinal))
            .ToList();

        if (matches.Count != 1)
            return Fail("ProjectNotFound", ResultDetails.Create().With("Project", command.Project));

        var project = matches[0];
        var wanted = new HashSet<string>(command.Namespaces, StringComparer.Ordinal);

        // Why: a namespace MSBuild also supplies (ImplicitUsings, <Using Include>) reappears in the
        // generated GlobalUsings.g.cs on the next build, so deleting the source line is a no-op dressed up
        // as a change. Refuse it and name the props file, rather than report success for nothing.
        var msbuildSupplied = await MsBuildSuppliedImports(project, command, cancellationToken).ConfigureAwait(false);
        var duplicate = command.Namespaces.FirstOrDefault(msbuildSupplied.Contains);
        if (duplicate is not null)
            return Fail("GlobalUsingIsMsBuildDuplicate", ResultDetails.Create()
                .With("Namespace", duplicate)
                .With("Project", project.Name)
                .With("PropsHint", "the Directory.Build.props that sets ImplicitUsings/<Using Include> for this project"));

        var targets = await SourceDirectives(project, command, wanted, cancellationToken).ConfigureAwait(false);
        if (targets.Count == 0)
            return Fail("NoGlobalUsingsMatched", ResultDetails.Create()
                .With("Namespaces", string.Join(", ", command.Namespaces))
                .With("Project", project.Name));

        var baseline = await DiagnosticDiff.Counts(project, cancellationToken).ConfigureAwait(false);

        // Why: same rule as the move commands — a preview reports, only a real run refuses. Here the
        // whole algorithm IS the diagnostic diff, so without a baseline there is nothing to preview
        // either; the refusal stands in both modes, but it must say which mode it is refusing.
        if (baseline is null && !command.AcceptUnverified)
            return Fail("ChangeCannotBeVerified", ResultDetails.Create()
                .With("ProjectCount", "1")
                .With("Detail", $"'{project.Name}' has no framework references, so no diagnostic diff from it would be meaningful"));

        // Why: with AcceptUnverified and no bindable baseline there is nothing to diff against, so every
        // comparison below degrades to "nothing appeared, nothing resolved" and the command does the
        // mechanical part — remove the directives, add the explicit imports — unchecked, as asked.
        baseline ??= new Dictionary<string, int>(StringComparer.Ordinal);

        var afterRemoval = await RemoveDirectives(solution, targets, cancellationToken).ConfigureAwait(false);

        var removedCounts = await DiagnosticDiff.Counts(
            afterRemoval.GetProject(project.Id)!, cancellationToken).ConfigureAwait(false) ?? new Dictionary<string, int>(StringComparer.Ordinal);

        var appeared = DiagnosticDiff.Appeared(baseline, removedCounts);
        var resolved = DiagnosticDiff.Appeared(removedCounts, baseline);

        // Why: a break inside generated code cannot be repaired by editing it — the next build rewrites
        // the file. Refusing is the only honest answer; repairing it would claim work that gets discarded.
        var unfixable = appeared.FirstOrDefault(a => command.IsGeneratedPath(DiagnosticDiff.PathOf(a)));
        if (unfixable is not null)
            return Fail("ChangeWouldNotCompile", ResultDetails.Create()
                .With("CollisionCount", "0")
                .With("UnresolvedCount", appeared.Count.ToString(CultureInfo.InvariantCulture))
                .With("FirstProblem", $"break lands in generated code, which cannot be repaired: {unfixable}"));

        var repaired = new List<string>();
        var withRepairs = await Repair(
            afterRemoval, project.Id, targets, appeared, command, repaired, cancellationToken).ConfigureAwait(false);

        var finalCounts = await DiagnosticDiff.Counts(
            withRepairs.GetProject(project.Id)!, cancellationToken).ConfigureAwait(false) ?? new Dictionary<string, int>(StringComparer.Ordinal);

        var surviving = DiagnosticDiff.Appeared(baseline, finalCounts);
        if (surviving.Count > 0 && !command.AcceptUnverified)
            return Fail("ChangeWouldNotCompile", ResultDetails.Create()
                .With("CollisionCount", "0")
                .With("UnresolvedCount", surviving.Count.ToString(CultureInfo.InvariantCulture))
                .With("FirstProblem", surviving[0]));

        var changedFiles = ChangedFiles(solution, withRepairs, project.Id, project.Name);
        var data = new RemoveGlobalUsingsData
        {
            Project = project.Name,
            Removed = targets.Select(t => t.Directive.ToString().Trim()).ToList(),
            Repaired = repaired,
            Resolved = resolved,
            EmptiedFiles = await EmptiedFiles(withRepairs, project.Id, cancellationToken).ConfigureAwait(false),
            WasDryRun = command.DryRun,
            FilesUnaffected = project.Documents.Count() - changedFiles.Count,
            AffectedFiles = changedFiles.Select(c => c.FilePath).ToList(),
        };

        var summary =
            $"Removed {data.Removed.Count} global using(s) from {project.Name}; " +
            $"{repaired.Count} file(s) gained an explicit using" +
            (resolved.Count > 0 ? $"; {resolved.Count} diagnostic(s) RESOLVED by the removal" : string.Empty) +
            (command.DryRun ? " (preview)" : string.Empty);

        RemoveGlobalUsingsTranslatorLog.Removed(Logger, project.Name, data.Removed.Count, repaired.Count);

        if (command.DryRun)
            return GenericResult<IRoslynCommandResult>.Success(
                new QueryResult<RemoveGlobalUsingsData>(summary, data));

        return GenericResult<IRoslynCommandResult>.Success(
            new MutationResult<RemoveGlobalUsingsData>(
                summary,
                withRepairs,
                changedFiles,
                Array.Empty<SymbolChange>(),
                Array.Empty<PathChange>(),
                data));
    }
#pragma warning restore MA0051

    private IGenericResult<IRoslynCommandResult> Fail(string code, ResultDetails details)
    {
        RemoveGlobalUsingsTranslatorLog.Failed(Logger, code);
        return GenericResult<IRoslynCommandResult>.Failure(RoslynResultCodes.ByName(code), details);
    }

    /// <summary>
    /// Reads the imports MSBuild supplies, by parsing the generated GlobalUsings file.
    /// </summary>
    private static async Task<HashSet<string>> MsBuildSuppliedImports(
        Project project,
        RemoveGlobalUsingsCommand command,
        CancellationToken cancellationToken)
    {
        var supplied = new HashSet<string>(StringComparer.Ordinal);

        foreach (var document in project.Documents)
        {
            if (!command.IsGeneratedDocument(document)) continue;
            if (document.FilePath?.IndexOf("GlobalUsings", StringComparison.OrdinalIgnoreCase) < 0) continue;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) continue;

            foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.None)) continue;
                if (directive.Name is null) continue;
                supplied.Add(Unqualified(directive.Name));
            }
        }

        return supplied;
    }

    /// <summary>
    /// Strips a <c>global::</c> alias qualifier from an import name.
    /// </summary>
    /// <param name="name">The name as written in the directive.</param>
    /// <returns>The namespace without the alias qualifier.</returns>
    /// <remarks>
    /// The SDK writes its generated imports as <c>global using global::System.Text;</c>, so comparing the
    /// raw text against a caller's "System.Text" never matches and every MSBuild-supplied namespace would
    /// slip past the duplicate check — the one thing that check exists to catch.
    ///
    /// Stripping the prefix textually rather than pattern-matching AliasQualifiedNameSyntax, because
    /// "global::System.Text" parses as a QUALIFIED name whose LEFT is the alias-qualified part — so a
    /// match on the outermost node never fires.
    /// </remarks>
    private static string Unqualified(NameSyntax name)
    {
        var text = name.ToString();
        return text.StartsWith("global::", StringComparison.Ordinal) ? text["global::".Length..] : text;
    }

    /// <summary>
    /// Finds the source-declared global using directives matching the requested namespaces.
    /// </summary>
    private static async Task<List<DirectiveTarget>> SourceDirectives(
        Project project,
        RemoveGlobalUsingsCommand command,
        HashSet<string> wanted,
        CancellationToken cancellationToken)
    {
        var targets = new List<DirectiveTarget>();

        foreach (var document in project.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command.IsGeneratedDocument(document)) continue;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) continue;

            foreach (var directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                if (directive.GlobalKeyword.IsKind(SyntaxKind.None)) continue;
                if (directive.Name is null) continue;
                if (!wanted.Contains(Unqualified(directive.Name))) continue;

                targets.Add(new DirectiveTarget(document.Id, directive));
            }
        }

        return targets;
    }

    /// <summary>
    /// Compiles the project and returns a diagnostic multiset, or null when it cannot bind at all.
    /// </summary>
    /// <remarks>
    /// Keyed on id + path + message and NEVER on source offsets: removing a directive shifts every offset
    /// in the file, so an offset-keyed diff would report the whole file as changed.
    /// </remarks>
    private static async Task<Solution> RemoveDirectives(
        Solution solution,
        List<DirectiveTarget> targets,
        CancellationToken cancellationToken)
    {
        var current = solution;

        foreach (var group in targets.GroupBy(t => t.DocumentId))
        {
            var document = current.GetDocument(group.Key);
            if (document is null) continue;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) continue;

            // Why: match by text rather than by node identity — the nodes were read from an earlier
            // snapshot of the tree, so they are not reference-equal to the nodes in this root.
            var texts = new HashSet<string>(group.Select(g => g.Directive.ToString().Trim()), StringComparer.Ordinal);
            var doomed = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Where(u => texts.Contains(u.ToString().Trim()))
                .ToList();

            if (doomed.Count == 0) continue;

            // Why: KeepLeadingTrivia, so a file header comment or a #pragma above the first directive
            // survives the removal instead of being deleted along with it.
            var updated = root.RemoveNodes(doomed, SyntaxRemoveOptions.KeepLeadingTrivia);
            if (updated is null) continue;

            current = current.WithDocumentSyntaxRoot(group.Key, updated);
        }

        return current;
    }

    /// <summary>
    /// Gives an explicit using to each file the removal broke.
    /// </summary>
    private static async Task<Solution> Repair(
        Solution solution,
        ProjectId projectId,
        List<DirectiveTarget> targets,
        IReadOnlyList<string> appeared,
        RemoveGlobalUsingsCommand command,
        List<string> repaired,
        CancellationToken cancellationToken)
    {
        var brokenPaths = new HashSet<string>(appeared.Select(DiagnosticDiff.PathOf), StringComparer.Ordinal);
        if (brokenPaths.Count == 0) return solution;

        var current = solution;

        foreach (var namespaceName in targets.Select(t => t.Directive.Name!.ToString()).Distinct(StringComparer.Ordinal))
        {
            foreach (var document in current.GetProject(projectId)!.Documents.ToList())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document.FilePath is null || !brokenPaths.Contains(document.FilePath)) continue;
                if (command.IsGeneratedDocument(document)) continue;

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is not CompilationUnitSyntax unit) continue;

                if (unit.DescendantNodes().OfType<UsingDirectiveSyntax>()
                    .Any(u => u.GlobalKeyword.IsKind(SyntaxKind.None)
                        && string.Equals(u.Name?.ToString(), namespaceName, StringComparison.Ordinal)))
                    continue;

                // Why: UsingDirective emits the `using` keyword with no trailing trivia, so the raw node
                // renders as "usingFdw.Sample;". NormalizeWhitespace puts the separator in.
                var updated = unit.AddUsings(
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(namespaceName))
                        .NormalizeWhitespace()
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));

                current = current.WithDocumentSyntaxRoot(document.Id, updated);
                repaired.Add($"{document.FilePath} += using {namespaceName};");
            }
        }

        return current;
    }

    private static async Task<List<string>> EmptiedFiles(
        Solution solution,
        ProjectId projectId,
        CancellationToken cancellationToken)
    {
        var emptied = new List<string>();

        foreach (var document in solution.GetProject(projectId)!.Documents)
        {
            if (document.FilePath is null) continue;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is not CompilationUnitSyntax unit) continue;
            if (unit.Usings.Count > 0 || unit.Members.Count > 0) continue;

            emptied.Add(document.FilePath);
        }

        return emptied;
    }

    private static List<FileChange> ChangedFiles(Solution before, Solution after, ProjectId projectId, string projectName)
    {
        var changed = new List<FileChange>();

        foreach (var document in after.GetProject(projectId)!.Documents)
        {
            if (document.FilePath is null) continue;

            var original = before.GetDocument(document.Id);
            if (original is null) continue;

            if (!original.TryGetText(out var originalText) || !document.TryGetText(out var currentText)) continue;
            if (!string.Equals(originalText.ToString(), currentText.ToString(), StringComparison.Ordinal))
                changed.Add(new FileChange(document.FilePath, FileChangeTypes.Modified, projectName));
        }

        return changed;
    }

    private sealed record DirectiveTarget(DocumentId DocumentId, UsingDirectiveSyntax Directive);
}
