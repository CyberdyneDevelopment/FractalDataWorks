using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Helpers;
using Fdw.Roslyn.Commands.Workspace.Results;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for <see cref="RepairMovedReferencesCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RepairMovedReferences")]
public sealed class RepairMovedReferencesTranslator
    : RoslynCommandTranslatorBase<RepairMovedReferencesCommand, IRoslynCommandResult>
{
    /// <summary>
    /// Diagnostics that mean "this name no longer resolves" — the shape a cross-assembly move produces.
    /// </summary>
    private static readonly string[] UnresolvedNameDiagnostics = { "CS0246", "CS0234", "CS0104" };

    /// <summary>
    /// Initializes a new instance of the <see cref="RepairMovedReferencesTranslator"/> class.
    /// </summary>
    public RepairMovedReferencesTranslator()
        : base("RepairMovedReferences", "Repairs unresolved-reference errors using the session change ledger")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<IRoslynCommandResult>> Translate(
        RepairMovedReferencesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));

        // Why: two sources, one repair path. The producer has a session ledger; a consumer who merely
        // bumped a version has only the published guide. Neither is guessed at — if the caller named a
        // guide it must be usable, and otherwise a ledger must be present.
        LedgerAssemblyIndex index;
        if (!string.IsNullOrWhiteSpace(command.GuidePath))
        {
            var guidePath = ResolveAgainstSolution(solution, command.GuidePath!);
            if (guidePath is null)
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("RelativeOutputPathNeedsSolutionPath"),
                    ResultDetails.Create().With("OutputPath", command.GuidePath!));

            if (!File.Exists(guidePath))
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("MigrationGuideNotUsable"),
                    ResultDetails.Create().With("GuidePath", guidePath).With("Problem", "does not exist"));

            var moves = MigrationGuideReader.ReadAssemblyMoves(guidePath);
            if (moves.Count == 0)
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("MigrationGuideNotUsable"),
                    ResultDetails.Create().With("GuidePath", guidePath)
                        .With("Problem", "records no cross-assembly moves, so it cannot explain a missing type"));

            index = new LedgerAssemblyIndex(moves);
        }
        else
        {
            if (command.Ledger is null)
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("LedgerNotAvailable"));

            index = new LedgerAssemblyIndex(command.Ledger.Entries);
        }
        var errors = await CollectUnresolvedNameErrors(solution, command.Scope, cancellationToken).ConfigureAwait(false);

        if (errors.Count == 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NoReferenceErrorsFound"),
                ResultDetails.Create().With("Scope", command.Scope ?? "(whole solution)"));

        var outcome = Classify(errors, index);

        // Why: one missing assembly usually produces many diagnostics in the same project. The caller is
        // approving REFERENCES, not error lines, so collapse to one decision per project+assembly.
        var proposals = outcome.Repairs
            .GroupBy(r => r.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .ToList();

        IReadOnlyList<string>? fromFile = null;
        if (!string.IsNullOrWhiteSpace(command.ApplyFromPath))
        {
            var planPath = ResolveAgainstSolution(solution, command.ApplyFromPath!);
            if (planPath is null)
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("RelativeOutputPathNeedsSolutionPath"),
                    ResultDetails.Create().With("OutputPath", command.ApplyFromPath!));

            if (!File.Exists(planPath))
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("RepairPlanNotFound"),
                    ResultDetails.Create().With("PlanPath", planPath));

            fromFile = ReferenceRepairPlanFile.ReadApprovedIds(planPath);
        }

        var rejected = proposals.Where(r => !IsApprovedBy(command, fromFile, r.Id)).ToList();
        var approved = proposals.Where(r => IsApprovedBy(command, fromFile, r.Id)).ToList();

        if (command.DryRun && !string.IsNullOrWhiteSpace(command.PreviewPath))
        {
            var previewPath = ResolveAgainstSolution(solution, command.PreviewPath!);
            if (previewPath is null)
                return GenericResult<IRoslynCommandResult>.Failure(
                    RoslynResultCodes.ByName("RelativeOutputPathNeedsSolutionPath"),
                    ResultDetails.Create().With("OutputPath", command.PreviewPath!));

            ReferenceRepairPlanFile.Write(previewPath, proposals, DateTimeOffset.Now);
        }

        var updated = solution;
        var pathChanges = new List<PathChange>();

        if (!command.DryRun)
        {
            updated = ApplyRepairs(solution, approved, pathChanges);
            if (command.WriteToDisk)
                WriteRepairsToDisk(updated, approved, command.VersionPin);
        }

        var data = new ReferenceRepairData
        {
            ErrorsExamined = errors.Count,
            RepairedCount = proposals.Count,
            UnresolvedCount = outcome.Unresolved.Count,
            ReferencesAdded = pathChanges.Count,
            WrittenToDiskCount = proposals.Count(r => r.WrittenToDisk),
            WasDryRun = command.DryRun,
            Repairs = proposals,
            Rejected = rejected,
            Unresolved = outcome.Unresolved,
        };

        var summary =
            $"{(command.DryRun ? "[DryRun] " : string.Empty)}{errors.Count} unresolved-reference error(s): " +
            $"{proposals.Count} reference(s) explained by the ledger, {outcome.Unresolved.Count} error(s) not; " +
            $"{approved.Count} approved, {rejected.Count} rejected, {pathChanges.Count} added in memory, " +
            $"{data.WrittenToDiskCount} written to disk";

        if (command.DryRun)
            return GenericResult<IRoslynCommandResult>.Success(
                new QueryResult<ReferenceRepairData>(summary, data));

        return GenericResult<IRoslynCommandResult>.Success(
            new MutationResult<ReferenceRepairData>(
                summary,
                updated,
                Array.Empty<FileChange>(),
                Array.Empty<SymbolChange>(),
                pathChanges,
                data));
    }

    private static async Task<List<UnresolvedNameError>> CollectUnresolvedNameErrors(
        Solution solution,
        string? scope,
        CancellationToken cancellationToken)
    {
        var errors = new List<UnresolvedNameError>();

        foreach (var project in solution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrWhiteSpace(scope) &&
                !project.Name.Contains(scope!, StringComparison.OrdinalIgnoreCase))
                continue;

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            foreach (var diagnostic in compilation.GetDiagnostics(cancellationToken))
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error) continue;
                if (!Array.Exists(UnresolvedNameDiagnostics, id => string.Equals(id, diagnostic.Id, StringComparison.Ordinal)))
                    continue;

                var name = MissingNameOf(diagnostic);
                if (string.IsNullOrEmpty(name)) continue;

                var span = diagnostic.Location.GetLineSpan();
                errors.Add(new UnresolvedNameError(
                    project,
                    diagnostic.Id,
                    span.Path,
                    span.StartLinePosition.Line + 1,
                    name!));
            }
        }

        return errors;
    }

    // Why: read the name from the SOURCE at the diagnostic location rather than parsing the message text.
    // Diagnostic messages are localised and their wording is not a contract; the span is.
    //
    // The span alone is not enough though. For a qualified name the compiler reports the first segment it
    // cannot bind — CS0234 on `Data` in `Fdw.Data.MsSql.Marker` — and a bare "Data" matches nothing in the
    // ledger. Ascending to the outermost enclosing qualified name recovers the full name the ledger keys on.
    private static string? MissingNameOf(Diagnostic diagnostic)
    {
        var tree = diagnostic.Location.SourceTree;
        if (tree is null || !diagnostic.Location.IsInSource) return null;

        var root = tree.GetRoot();
        SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        if (node is null) return null;

        while (node.Parent is QualifiedNameSyntax || node.Parent is AliasQualifiedNameSyntax)
            node = node.Parent;

        return node.ToString();
    }

    private static ClassifiedErrors Classify(IReadOnlyList<UnresolvedNameError> errors, LedgerAssemblyIndex index)
    {
        var outcome = new ClassifiedErrors();

        foreach (var error in errors)
        {
            var lookup = index.Resolve(error.MissingName);

            if (!lookup.IsResolved)
            {
                outcome.Unresolved.Add(new UnresolvedReferenceError
                {
                    Project = error.Project.Name,
                    DiagnosticId = error.DiagnosticId,
                    FilePath = error.FilePath,
                    Line = error.Line,
                    MissingName = error.MissingName,
                    Reason = lookup.Reason ?? "unknown",
                });
                continue;
            }

            // Why: a project cannot reference itself, and MSBuild rejects the csproj outright if you try.
            // This fires when the moved type's NEW assembly IS the project reporting the error — the type
            // landed here and the diagnostic is about something else entirely, so proposing a reference is
            // not just useless, it produces a plan that breaks the build when applied verbatim.
            if (string.Equals(error.Project.AssemblyName, lookup.Change!.NewAssembly, StringComparison.Ordinal))
                continue;

            outcome.Repairs.Add(new ReferenceRepair
            {
                Id = error.Project.Name + "=>" + lookup.Change.NewAssembly,
                Project = error.Project.Name,
                DiagnosticId = error.DiagnosticId,
                FilePath = error.FilePath,
                Line = error.Line,
                MissingName = error.MissingName,
                LedgerMatch = lookup.Change.NewFullyQualifiedName,
                RequiredAssembly = lookup.Change.NewAssembly!,
            });
        }

        return outcome;
    }

    // Why: rejection wins over approval so a thumbs-down is never overridden by a broad ApproveAll —
    // an explicit veto is the more specific instruction. A pruned plan file supersedes both, because a
    // reviewed file is the most explicit instruction of the three.
    private static bool IsApprovedBy(
        RepairMovedReferencesCommand command,
        IReadOnlyList<string>? fromFile,
        string id)
    {
        if (fromFile is not null)
            return fromFile.Any(f => string.Equals(f, id, StringComparison.Ordinal));

        if (command.Reject is not null &&
            Array.Exists(command.Reject, r => string.Equals(r, id, StringComparison.Ordinal)))
            return false;

        return command.ApproveAll ||
               (command.Approve is not null &&
                Array.Exists(command.Approve, a => string.Equals(a, id, StringComparison.Ordinal)));
    }

    private static string? ResolveAgainstSolution(Solution solution, string path)
    {
        if (Path.IsPathRooted(path)) return path;

        var directory = solution.FilePath is null ? null : Path.GetDirectoryName(solution.FilePath);
        return string.IsNullOrEmpty(directory) ? null : Path.GetFullPath(Path.Combine(directory!, path));
    }

    /// <summary>
    /// Writes each approved repair into the consuming project file.
    /// </summary>
    /// <remarks>
    /// A repair whose assembly is a project in the solution becomes a ProjectReference; anything else is
    /// a package, which needs an explicit VersionPin rather than a guessed version.
    /// </remarks>
    private static void WriteRepairsToDisk(
        Solution solution,
        IReadOnlyList<ReferenceRepair> approved,
        string? versionPin)
    {
        foreach (var repair in approved)
        {
            var consumer = solution.Projects.FirstOrDefault(p =>
                string.Equals(p.Name, repair.Project, StringComparison.Ordinal));

            if (consumer?.FilePath is null)
            {
                repair.WriteDetail = $"consuming project '{repair.Project}' has no project file on disk";
                continue;
            }

            var provider = solution.Projects.FirstOrDefault(p =>
                string.Equals(p.AssemblyName, repair.RequiredAssembly, StringComparison.Ordinal));

            var edit = provider?.FilePath is not null
                ? ProjectFileEditor.AddProjectReference(consumer.FilePath, provider.FilePath)
                : WritePackageReference(consumer.FilePath, repair.RequiredAssembly, versionPin);

            repair.ReferenceKind = provider?.FilePath is not null ? "ProjectReference" : "PackageReference";
            repair.WrittenToDisk = edit.Success && edit.Changed;
            repair.WriteDetail = edit.Detail;
        }
    }

    private static ProjectFileEditResult WritePackageReference(
        string consumerProjectPath,
        string packageId,
        string? versionPin)
    {
        if (string.IsNullOrWhiteSpace(versionPin))
            return ProjectFileEditResult.Failed(
                $"'{packageId}' is not a project in this solution, so it needs a PackageReference — pass VersionPin " +
                "with a literal version or an MSBuild property such as \"$(FdwVersion)\"");

        var propsPath = ProjectFileEditor.FindPackagesProps(Path.GetDirectoryName(consumerProjectPath));

        return ProjectFileEditor.AddPackageReference(
            consumerProjectPath,
            packageId,
            versionPin!,
            ProjectFileEditor.IsCentralPackageManagement(propsPath),
            propsPath);
    }

    private static Solution ApplyRepairs(
        Solution solution,
        IReadOnlyList<ReferenceRepair> repairs,
        List<PathChange> pathChanges)
    {
        var updated = solution;

        foreach (var repair in repairs)
        {
            var consumer = updated.Projects.FirstOrDefault(p =>
                string.Equals(p.Name, repair.Project, StringComparison.Ordinal));
            var provider = updated.Projects.FirstOrDefault(p =>
                string.Equals(p.AssemblyName, repair.RequiredAssembly, StringComparison.Ordinal));

            // The assembly may not be a project in this solution (a packaged dependency). Report the
            // requirement rather than inventing a PackageReference the caller never asked for.
            if (consumer is null || provider is null || consumer.Id == provider.Id) continue;
            if (consumer.ProjectReferences.Any(r => r.ProjectId == provider.Id))
            {
                repair.Applied = true;
                continue;
            }

            updated = updated.AddProjectReference(consumer.Id, new ProjectReference(provider.Id));
            repair.Applied = true;
            pathChanges.Add(new PathChange(consumer.Name, provider.Name, "ProjectReferenceAdded"));
        }

        return updated;
    }

    private sealed class UnresolvedNameError
    {
        public UnresolvedNameError(Project project, string diagnosticId, string filePath, int line, string missingName)
        {
            Project = project;
            DiagnosticId = diagnosticId;
            FilePath = filePath;
            Line = line;
            MissingName = missingName;
        }

        public Project Project { get; }

        public string DiagnosticId { get; }

        public string FilePath { get; }

        public int Line { get; }

        public string MissingName { get; }
    }

    private sealed class ClassifiedErrors
    {
        public List<ReferenceRepair> Repairs { get; } = new();

        public List<UnresolvedReferenceError> Unresolved { get; } = new();
    }
}
