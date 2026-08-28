using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Helpers;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Helpers;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for <see cref="MoveNamespaceCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "MoveNamespace")]
public sealed class MoveNamespaceTranslator
    : RoslynCommandTranslatorBase<MoveNamespaceCommand, IRoslynCommandResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveNamespaceTranslator"/> class.
    /// </summary>
    public MoveNamespaceTranslator()
        : base("MoveNamespace", "Renames a namespace and every reference to it across the solution")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IRoslynCommandResult>> Translate(
        MoveNamespaceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            MoveNamespaceTranslatorLog.CommandCannotBeNull(Logger);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));
        }

        if (string.IsNullOrWhiteSpace(command.OldNamespace) || string.IsNullOrWhiteSpace(command.NewNamespace))
        {
            MoveNamespaceTranslatorLog.NamespaceRequired(Logger);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NamespaceRequired"));
        }

        MoveNamespaceTranslatorLog.Moving(Logger, command.OldNamespace, command.NewNamespace, command.DryRun);

        if (string.Equals(command.OldNamespace, command.NewNamespace, StringComparison.Ordinal))
        {
            MoveNamespaceTranslatorLog.TargetSameAsCurrent(Logger, command.OldNamespace);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TargetSameAsCurrent"),
                ResultDetails.Create().With("Namespace", command.OldNamespace));
        }

        if (!solution.Projects.Any(p => TestProjectDetector.IsTestProject(p.Name)))
        {
            MoveNamespaceTranslatorLog.TestProjectsNotLoaded(Logger);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TestProjectsNotLoaded"),
                ResultDetails.Create().With("ExcludedCount", "all"));
        }

        var rewrite = await RewriteSolution(solution, command, cancellationToken).ConfigureAwait(false);

        if (rewrite.ChangedFiles.Count == 0)
        {
            MoveNamespaceTranslatorLog.NoTypesMatchedSelector(Logger, command.OldNamespace);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NoTypesMatchedSelector"),
                ResultDetails.Create().With("Selector", command.OldNamespace));
        }

        var affectedProjectIds = ChangedProjectIds(rewrite);
        var baseline = await DiagnosticDiff.Counts(
            affectedProjectIds.Select(solution.GetProject).Where(p => p is not null).Select(p => p!),
            cancellationToken).ConfigureAwait(false);

        var probed = await TypeCollisionProbe
            .Probe(rewrite.Solution, affectedProjectIds, Array.Empty<string>(), command, baseline, cancellationToken)
            .ConfigureAwait(false);


        var unverifiable = probed.Where(b => string.Equals(b.Kind, "ProbeUnavailable", StringComparison.Ordinal)).ToList();
        if (!command.DryRun && !command.AcceptUnverified && unverifiable.Count > 0)
        {
            MoveNamespaceTranslatorLog.ChangeCannotBeVerified(Logger, command.NewNamespace, unverifiable.Count);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("ChangeCannotBeVerified"),
                ResultDetails.Create()
                    .With("ProjectCount", unverifiable.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("Detail", unverifiable[0].Detail));
        }

        var collisions = probed.Count(b => string.Equals(b.Kind, "TypeCollision", StringComparison.Ordinal));
        var unresolved = probed.Count(b => string.Equals(b.Kind, "UnresolvedReference", StringComparison.Ordinal));

        if (!command.DryRun && !command.AcceptUnverified && probed.Count > 0)
        {
            MoveNamespaceTranslatorLog.ChangeWouldNotCompile(Logger, command.NewNamespace, collisions, unresolved);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("ChangeWouldNotCompile"),
                ResultDetails.Create()
                    .With("CollisionCount", collisions.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("UnresolvedCount", unresolved.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("First", probed[0].Detail));
        }

        var data = new MoveNamespaceData
        {
            Breaks = probed,
            CollisionCount = collisions,
            UnresolvedCount = unresolved,
            OldNamespace = command.OldNamespace,
            NewNamespace = command.NewNamespace,
            DocumentsChanged = rewrite.ChangedFiles.Count,
            ReferencesRewritten = rewrite.ReferenceCount,
            TypesRenamed = rewrite.DeclaredTypes.Count,
            TypeOptionIdsChanged = rewrite.TypeOptionCount,
            WasDryRun = command.DryRun,
            ConsumerImpact = rewrite.TypeOptionCount > 0
                ? $"CONSUMER-BREAKING: {rewrite.DeclaredTypes.Count} type(s) change fully-qualified name; {rewrite.TypeOptionCount} carry a TypeOption whose FNV-1a Id changes with the FQN."
                : $"CONSUMER-BREAKING: {rewrite.DeclaredTypes.Count} type(s) change fully-qualified name. No TypeOption Ids affected.",
            AffectedFiles = rewrite.ChangedFiles.Select(c => c.FilePath).ToList(),
        };

        var summary =
            $"{(command.DryRun ? "[DryRun] " : string.Empty)}Renamed namespace '{command.OldNamespace}' to " +
            $"'{command.NewNamespace}': {rewrite.ReferenceCount} reference(s) across {rewrite.ChangedFiles.Count} file(s)";

        MoveNamespaceTranslatorLog.Moved(Logger, command.OldNamespace, command.NewNamespace, rewrite.ReferenceCount, rewrite.ChangedFiles.Count, command.DryRun);

        if (command.DryRun)
            return GenericResult<IRoslynCommandResult>.Success(
                new QueryResult<MoveNamespaceData>(summary, data));

        return GenericResult<IRoslynCommandResult>.Success(
            new MutationResult<MoveNamespaceData>(
                summary,
                rewrite.Solution,
                rewrite.ChangedFiles,
                rewrite.SymbolChanges,
                Array.Empty<PathChange>(),
                data));
    }

    // Only the projects the rewrite actually touched — compiling the untouched remainder of a
    // 444-project solution to learn nothing would make the preview unusable.
    private static List<ProjectId> ChangedProjectIds(RewriteOutcome rewrite)
    {
        var names = new HashSet<string>(rewrite.ChangedFiles.Select(c => c.ProjectName), StringComparer.Ordinal);

        return rewrite.Solution.Projects
            .Where(p => names.Contains(p.Name))
            .Select(p => p.Id)
            .ToList();
    }

    private static async Task<RewriteOutcome> RewriteSolution(
        Solution solution,
        MoveNamespaceCommand command,
        CancellationToken cancellationToken)
    {
        var outcome = new RewriteOutcome(solution);

        foreach (var projectId in solution.ProjectIds)
        {
            var project = solution.GetProject(projectId);
            if (project is null) continue;

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (command.IsGeneratedDocument(document)) continue;

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is null) continue;

                var matches = MatchingNames(root, command.OldNamespace);
                if (matches.Count == 0) continue;

                var replacement = SyntaxFactory.ParseName(command.NewNamespace);
                var newRoot = root.ReplaceNodes(
                    matches,
                    (original, _) => replacement.WithTriviaFrom(original));

                outcome.Solution = outcome.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
                outcome.ReferenceCount += matches.Count;
                outcome.ChangedFiles.Add(new FileChange(
                    document.FilePath ?? string.Empty,
                    FileChangeTypes.Modified,
                    project.Name)
                {
                    TextChangeCount = matches.Count,
                });

                await RecordDeclaredTypes(document, command, project, outcome, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return outcome;
    }

    private static async Task RecordDeclaredTypes(
        Document document,
        MoveNamespaceCommand command,
        Project project,
        RewriteOutcome outcome,
        CancellationToken cancellationToken)
    {
        var declarations = await TypeDeclarationReader.Read(document, cancellationToken).ConfigureAwait(false);

        foreach (var declaration in declarations)
        {
            if (!string.Equals(declaration.Namespace, command.OldNamespace, StringComparison.Ordinal))
                continue;

            outcome.DeclaredTypes.Add(declaration.TypeName);
            if (declaration.IsTypeOption) outcome.TypeOptionCount++;

            outcome.SymbolChanges.Add(new SymbolChange(
                command.OldNamespace + "." + declaration.TypeName,
                command.NewNamespace + "." + declaration.TypeName,
                SymbolChangeTypes.Renamed.Name,
                "NamedType",
                document.FilePath,
                document.FilePath,
                project.AssemblyName,
                project.AssemblyName,
                NamespaceLayout.RelativePosition(project, document.FilePath)));
        }
    }

    private static List<NameSyntax> MatchingNames(SyntaxNode root, string oldNamespace) =>
        root.DescendantNodes(descendIntoTrivia: true)
            .OfType<NameSyntax>()
            .Where(name => string.Equals(name.ToString(), oldNamespace, StringComparison.Ordinal))
            .ToList();

    private sealed class RewriteOutcome
    {
        public RewriteOutcome(Solution solution) => Solution = solution;

        public Solution Solution { get; set; }

        public int ReferenceCount { get; set; }

        public int TypeOptionCount { get; set; }

        public List<FileChange> ChangedFiles { get; } = new();

        public List<SymbolChange> SymbolChanges { get; } = new();

        public List<string> DeclaredTypes { get; } = new();
    }
}
