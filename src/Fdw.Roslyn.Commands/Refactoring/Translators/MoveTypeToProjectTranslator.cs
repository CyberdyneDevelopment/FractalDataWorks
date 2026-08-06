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
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Helpers;
using Fdw.Roslyn.Commands.Refactoring.Logging;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Workspace.Helpers;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for <see cref="MoveTypeToProjectCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "MoveTypeToProject")]
public sealed class MoveTypeToProjectTranslator
    : RoslynCommandTranslatorBase<MoveTypeToProjectCommand, IRoslynCommandResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTypeToProjectTranslator"/> class.
    /// </summary>
    public MoveTypeToProjectTranslator()
        : base("MoveTypeToProject", "Moves a namespace's documents into the project that namespace names")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear flow: resolve target, collect documents, compute references, move.
    public override async Task<IGenericResult<IRoslynCommandResult>> Translate(
        MoveTypeToProjectCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));

        if (string.IsNullOrWhiteSpace(command.Namespace))
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NamespaceRequired"));

        var sources = await CollectDocuments(solution, command, cancellationToken).ConfigureAwait(false);
        if (sources.Count == 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NoTypesMatchedSelector"),
                ResultDetails.Create().With("Selector", command.Namespace));

        var sourceProject = sources[0].Project;
        var targetName = command.TargetProject ?? command.Namespace;
        var targetProject = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, targetName, StringComparison.Ordinal));

        // Why: "move it into the project its namespace names" is only actionable when that project exists.
        // Silently redirecting to the nearest ancestor project would send the caller somewhere nobody chose,
        // so the failure names the two real options instead.
        if (targetProject is null)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TargetProjectDoesNotExist"),
                ResultDetails.Create()
                    .With("TargetProject", targetName)
                    .With("Namespace", command.Namespace)
                    .With("CurrentProject", sourceProject.Name)
                    .With("TypeCount", sources.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        if (string.Equals(targetProject.Id.ToString(), sourceProject.Id.ToString(), StringComparison.Ordinal))
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TargetSameAsCurrent"),
                ResultDetails.Create().With("Namespace", command.Namespace));

        var occupied = OccupiedTargetPath(solution, targetProject, command.Namespace, sources);
        if (occupied is not null)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TargetPathOccupied"),
                ResultDetails.Create().With("TargetPath", occupied));

        var required = await ComputeRequiredReferences(sources, sourceProject, cancellationToken).ConfigureAwait(false);
        var droppable = await ComputeDroppableReferences(sources, sourceProject, required, cancellationToken).ConfigureAwait(false);

        var breaks = new List<BreakFinding>();
        breaks.AddRange(await MoveBreakScanner
            .ScanMovedDocuments(sources.Select(s => s.Document).ToList(), cancellationToken).ConfigureAwait(false));
        breaks.AddRange(await MoveBreakScanner
            .ScanAssemblyNameReferences(solution, sourceProject.AssemblyName, command, cancellationToken).ConfigureAwait(false));

        var move = await ApplyMove(solution, sources, targetProject, command, cancellationToken).ConfigureAwait(false);
        FixProjectReferences(move, sourceProject.Id, targetProject.Id, required, droppable);

        // Why: ask the COMPILER whether the move binds, rather than inferring from names or paths. A path
        // check answers "is that file taken", which misses a type declared in a differently-named file, a
        // partial, a generated source, and the namespace-segment-that-is-also-a-type case where A.B.C
        // stops resolving because B binds to a type. Scoped to the two affected projects, not the solution.
        var movedTypeNames = sources.SelectMany(s => s.Declarations.Select(d => d.TypeName)).Distinct(StringComparer.Ordinal).ToList();

        // Baselined against the original solution for the same two projects — see MoveTypesToNamespace.
        var probeBaseline = await DiagnosticDiff.Counts(
            new[] { sourceProject.Id, targetProject.Id }
                .Select(solution.GetProject).Where(p => p is not null).Select(p => p!),
            cancellationToken).ConfigureAwait(false);

        var collisions = await TypeCollisionProbe
            .Probe(move.Solution, new[] { sourceProject.Id, targetProject.Id }, movedTypeNames, command,
                probeBaseline, cancellationToken)
            .ConfigureAwait(false);
        var unverifiable = collisions.Where(b => string.Equals(b.Kind, "ProbeUnavailable", StringComparison.Ordinal)).ToList();
        // Why: a preview writes nothing and cannot break anything, so there is nothing here for a refusal
        // to protect — and refusing it removes the caller's only way to SEE what the change would do.
        // Fail-loud is satisfied by REPORTING the unverifiable projects in the result, which the preview
        // does; it is a real run, which would write an unchecked rewrite to disk and record it in the
        // ledger as though it had been verified, that must still refuse.
        if (!command.DryRun && !command.AcceptUnverified && unverifiable.Count > 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("ChangeCannotBeVerified"),
                ResultDetails.Create()
                    .With("ProjectCount", unverifiable.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("Detail", unverifiable[0].Detail));

        breaks.AddRange(collisions);
        breaks.AddRange(move.Cycles);

        // Applying a move already known to collide would knowingly break the build, so a real run refuses.
        // A preview still reports them, which is the whole point of previewing.
        if (!command.DryRun && !command.AcceptUnverified && (collisions.Count > 0 || move.Cycles.Count > 0))
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("MoveWouldCollide"),
                ResultDetails.Create()
                    .With("CollisionCount", collisions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("FirstCollision", collisions[0].Detail));

        // Why: a symbol closure answers "what does this code USE" and cannot answer "what does it need to
        // EXIST". Generator-backed members only appear when the generator runs, and the generator is an
        // ANALYZER reference, so it is invisible to symbol scanning — it has to come from the attributes.
        var generators = await GeneratorRequirementScanner
            .Scan(sources.Select(x => x.Document).ToList(), cancellationToken).ConfigureAwait(false);

        // Why: a correct reference closure is still unusable if the target cannot legally reference it.
        // netstandard2.0 cannot reference net10.0 — NU1201 — and the closure being right makes that
        // failure more confusing, not less, because everything the tool reported was accurate.
        var incompatible = IncompatibleReferences(move.ReferencesToWrite, targetProject);

        var referencesWritten = command.DryRun
            ? new List<string>()
            : WriteTargetReferences(move, targetProject, cancellationToken);

        var data = new MoveTypeToProjectData
        {
            Namespace = command.Namespace,
            SourceProject = sourceProject.Name,
            TargetProject = targetProject.Name,
            DocumentsMoved = sources.Count,
            WasDryRun = command.DryRun,
            ConsumerImpact =
                "NOT consumer-breaking: fully-qualified names are unchanged, so TypeOption Ids (FNV-1a of the FQN) are unchanged. " +
                "A consumer hitting CS0246 needs a reference to the new package, not a code change.",
            RequiredReferences = required,
            ReferencesWritten = referencesWritten,
            RequiredGenerators = generators,
            IncompatibleReferences = incompatible,
            DroppableReferences = droppable,
            Breaks = breaks,
            MovedFiles = move.PathChanges.Select(p => $"{p.OldPath} -> {p.NewPath}").ToList(),
        };

        var summary =
            $"{(command.DryRun ? "[DryRun] " : string.Empty)}Moved {sources.Count} document(s) in '{command.Namespace}' " +
            $"from '{sourceProject.Name}' to '{targetProject.Name}'; {required.Count} required reference(s), " +
            $"{droppable.Count} droppable, {breaks.Count} break finding(s)";

        if (command.DryRun)
            return GenericResult<IRoslynCommandResult>.Success(
                new QueryResult<MoveTypeToProjectData>(summary, data));

        return GenericResult<IRoslynCommandResult>.Success(
            new MutationResult<MoveTypeToProjectData>(
                summary,
                move.Solution,
                move.ChangedFiles,
                move.SymbolChanges,
                move.PathChanges,
                data));
    }
#pragma warning restore MA0051

    private async Task<List<DocumentInProject>> CollectDocuments(
        Solution solution,
        MoveTypeToProjectCommand command,
        CancellationToken cancellationToken)
    {
        var results = new List<DocumentInProject>();

        // Why: counted separately from the other rejections. A document that declares a namespace
        // BENEATH the selector is the one rejection a caller almost never intends — it is how a move
        // silently takes half a package — so it is reported on its own rather than buried in a Trace
        // stream nobody had running at the time.
        var stranded = 0;

        foreach (var project in solution.Projects)
        {
            if (command.SourceProject is not null &&
                !string.Equals(project.Name, command.SourceProject, StringComparison.Ordinal))
                continue;

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (command.IsGeneratedDocument(document))
                {
                    MoveCommandLog.SelectionRejected(Logger, "generated file", document.FilePath);
                    continue;
                }

                var declarations = await TypeDeclarationReader.Read(document, cancellationToken).ConfigureAwait(false);
                if (declarations.Count == 0)
                {
                    MoveCommandLog.SelectionRejected(Logger, "declares no types", document.FilePath);
                    continue;
                }

                if (!declarations.Any(d => Matches(d.Namespace, command.Namespace, command.IncludeSubNamespaces)))
                {
                    if (!command.IncludeSubNamespaces
                        && declarations.Any(d => Matches(d.Namespace, command.Namespace, includeSubNamespaces: true)))
                    {
                        stranded++;
                        MoveCommandLog.SelectionRejected(Logger, "sub-namespace, not included", document.FilePath);
                    }
                    else
                    {
                        MoveCommandLog.SelectionRejected(Logger, "namespace does not match", document.FilePath);
                    }

                    continue;
                }

                if (command.SkipTypes is not null &&
                    declarations.Any(d => Array.Exists(command.SkipTypes,
                        skip => string.Equals(skip, d.TypeName, StringComparison.Ordinal))))
                {
                    MoveCommandLog.SelectionRejected(Logger, "named in SkipTypes", document.FilePath);
                    continue;
                }

                results.Add(new DocumentInProject(document, project, declarations));
            }
        }

        if (stranded > 0)
        {
            MoveCommandLog.SubNamespacesStranded(Logger, command.Namespace, stranded);
        }

        MoveCommandLog.SelectionResolved(Logger, command.Name, results.Count, stranded);

        return results;
    }

    /// <summary>
    /// Determines whether a declared namespace is selected by the command's namespace.
    /// </summary>
    /// <param name="declared">The namespace the type declares.</param>
    /// <param name="selector">The namespace the caller asked for.</param>
    /// <param name="includeSubNamespaces">Whether nested namespaces are included.</param>
    /// <returns><see langword="true"/> when the type should move.</returns>
    /// <remarks>
    /// "Move Fdw.Data.MsSql" naturally reads as the namespace AND everything under it — that is what
    /// nesting means — so exact matching quietly stranded Fdw.Data.MsSql.Results, .Configurations,
    /// .Logging and .Translators in the old project. The split then looked done while 53 of 203 files had
    /// not moved, and one of those left-behind sub-namespaces was where the only real reference drop of
    /// the exercise came from.
    ///
    /// Prefix matching is dotted so Fdw.Data.MsSqlOther is not treated as nested inside Fdw.Data.MsSql.
    /// </remarks>
    private static bool Matches(string declared, string selector, bool includeSubNamespaces)
    {
        if (string.Equals(declared, selector, StringComparison.Ordinal)) return true;

        return includeSubNamespaces
            && declared.StartsWith(selector + ".", StringComparison.Ordinal);
    }

    private static string? OccupiedTargetPath(
        Solution solution,
        Project targetProject,
        string namespaceName,
        IReadOnlyList<DocumentInProject> sources)
    {
        foreach (var source in sources)
        {
            var typeName = source.Declarations[0].TypeName;
            var expected = NamespaceLayout.ExpectedPath(targetProject, namespaceName, typeName);
            if (expected is null) continue;

            if (solution.GetDocumentIdsWithFilePath(expected).Length > 0) return expected;
        }

        return null;
    }

    /// <summary>
    /// Finds references the target cannot legally take because of its target framework.
    /// </summary>
    /// <param name="referenced">The projects the target must reference.</param>
    /// <param name="target">The project receiving the types.</param>
    /// <returns>A line per incompatible reference, naming both frameworks.</returns>
    /// <remarks>
    /// Compares the parsed TargetFramework of each side. A netstandard2.0 project referencing a net10.0
    /// one fails restore with NU1201 — and because the reference closure itself is correct, nothing else
    /// in the output hints at the cause.
    /// </remarks>
    private static List<string> IncompatibleReferences(IReadOnlyList<Project> referenced, Project target)
    {
        var incompatible = new List<string>();
        var targetFramework = FrameworkOf(target);
        if (targetFramework is null || !targetFramework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
            return incompatible;

        foreach (var project in referenced)
        {
            var framework = FrameworkOf(project);
            if (framework is null) continue;
            if (framework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

            incompatible.Add(
                $"{project.Name} targets {framework}, but {target.Name} targets {targetFramework} — " +
                "restore will fail with NU1201. Retarget the receiving project or leave these types behind.");
        }

        return incompatible;
    }

    /// <summary>
    /// Reads a project's target framework from its compilation options moniker, if available.
    /// </summary>
    /// <param name="project">The project to inspect.</param>
    /// <returns>The framework moniker, or <see langword="null"/> when it cannot be determined.</returns>
    private static string? FrameworkOf(Project project)
    {
        // Why: Roslyn does not surface the TFM directly, but the MSBuild-loaded project name carries it
        // for multi-targeted projects and the output path contains it otherwise.
        var separator = project.Name.IndexOf('(', StringComparison.Ordinal);
        if (separator > 0)
            return project.Name[(separator + 1)..].TrimEnd(')');

        var output = project.OutputFilePath;
        if (string.IsNullOrEmpty(output)) return null;

        var parts = output!.Replace('\\', '/').Split('/');
        return parts.Length >= 2 ? parts[^2] : null;
    }

    /// <summary>
    /// Writes the target project's computed reference closure into its csproj.
    /// </summary>
    /// <param name="move">The move outcome carrying the wired references.</param>
    /// <param name="target">The project receiving the types.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A line per reference describing what was written.</returns>
    /// <remarks>
    /// Written here rather than left to ApplyWorkspaceChanges because that path persists DOCUMENT text,
    /// and a ProjectReference is not a document. This is the same ProjectFileEditor the repair command
    /// uses, so both paths edit a csproj identically.
    /// </remarks>
    private static List<string> WriteTargetReferences(
        MoveOutcome move,
        Project target,
        CancellationToken cancellationToken)
    {
        var written = new List<string>();
        if (target.FilePath is null) return written;

        foreach (var referenced in move.ReferencesToWrite)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (referenced.FilePath is null) continue;

            var edit = ProjectFileEditor.AddProjectReference(target.FilePath, referenced.FilePath);
            written.Add($"{referenced.Name}: {(edit.Success ? (edit.Changed ? "written" : "already present") : edit.Detail)}");
        }

        return written;
    }

    private static async Task<List<ReferenceRequirement>> ComputeRequiredReferences(
        IReadOnlyList<DocumentInProject> sources,
        Project sourceProject,
        CancellationToken cancellationToken)
    {
        var merged = new Dictionary<string, ReferenceRequirement>(StringComparer.Ordinal);

        foreach (var source in sources)
        {
            var usage = await AssemblyUsageScanner
                .Scan(source.Document, sourceProject.AssemblyName, cancellationToken).ConfigureAwait(false);

            foreach (var entry in usage.Values)
            {
                if (merged.TryGetValue(entry.Assembly, out var existing))
                {
                    existing.SymbolCount += entry.SymbolCount;
                    continue;
                }

                merged[entry.Assembly] = new ReferenceRequirement
                {
                    Assembly = entry.Assembly,
                    BecauseOf = entry.ExampleSymbol,
                    SymbolCount = entry.SymbolCount,
                };
            }
        }

        return merged.Values.OrderByDescending(r => r.SymbolCount).ThenBy(r => r.Assembly, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Works out what the source project can shed once the documents leave.
    /// </summary>
    /// <remarks>
    /// The payoff metric. A reference is droppable only when the moved documents used it and NOTHING left
    /// behind does. Reporting zero honestly matters more than reporting a number — a move that sheds
    /// nothing is a move that bought nothing, and presenting it as a success would hide that.
    /// </remarks>
    private static async Task<List<string>> ComputeDroppableReferences(
        IReadOnlyList<DocumentInProject> sources,
        Project sourceProject,
        IReadOnlyList<ReferenceRequirement> required,
        CancellationToken cancellationToken)
    {
        var movedIds = new HashSet<DocumentId>(sources.Select(s => s.Document.Id));
        var stillUsed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var document in sourceProject.Documents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (movedIds.Contains(document.Id)) continue;

            var usage = await AssemblyUsageScanner
                .Scan(document, sourceProject.AssemblyName, cancellationToken).ConfigureAwait(false);

            foreach (var assembly in usage.Keys) stillUsed.Add(assembly);
        }

        var referencedProjectNames = new HashSet<string>(
            sourceProject.ProjectReferences
                .Select(r => sourceProject.Solution.GetProject(r.ProjectId)?.AssemblyName)
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!),
            StringComparer.Ordinal);

        return required
            .Select(r => r.Assembly)
            .Where(assembly => !stillUsed.Contains(assembly) && referencedProjectNames.Contains(assembly))
            .OrderBy(assembly => assembly, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task<MoveOutcome> ApplyMove(
        Solution solution,
        IReadOnlyList<DocumentInProject> sources,
        Project targetProject,
        MoveTypeToProjectCommand command,
        CancellationToken cancellationToken)
    {
        var outcome = new MoveOutcome(solution);

        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var typeName = source.Declarations[0].TypeName;
            var newPath = NamespaceLayout.ExpectedPath(targetProject, command.Namespace, typeName);
            if (newPath is null) continue;

            var text = await source.Document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var folders = NamespaceLayout.RelativeFolders(command.Namespace, targetProject.Name) ?? Array.Empty<string>();

            outcome.Solution = outcome.Solution.RemoveDocument(source.Document.Id);

            var added = outcome.Solution
                .GetProject(targetProject.Id)!
                .AddDocument(typeName + ".cs", text, folders, newPath);

            outcome.Solution = added.Project.Solution;

            outcome.ChangedFiles.Add(new FileChange(newPath, FileChangeTypes.Added, targetProject.Name) { TextChangeCount = 1 });
            outcome.PathChanges.Add(new PathChange(source.Document.FilePath ?? string.Empty, newPath, "Document"));

            foreach (var declaration in source.Declarations)
            {
                var fqn = declaration.Namespace + "." + declaration.TypeName;
                outcome.SymbolChanges.Add(new SymbolChange(
                    fqn,
                    fqn,
                    SymbolChangeTypes.Moved.Name,
                    "NamedType",
                    source.Document.FilePath,
                    newPath,
                    source.Project.AssemblyName,
                    targetProject.AssemblyName,
                    NamespaceLayout.RelativePosition(targetProject, newPath)));
            }
        }

        return outcome;
    }

    /// <summary>
    /// Adds to the target the references its new documents need, and removes from the source the ones it
    /// no longer does.
    /// </summary>
    /// <remarks>
    /// In-memory only, like every other mutation here — the workspace commit writes documents, so the
    /// corresponding .csproj edits are reported through PathChanges for the caller to apply. Doing the
    /// in-memory half matters anyway: without it the moved documents do not compile in their new project,
    /// and any later command in the same session would analyse a solution that cannot build.
    /// </remarks>
    private static void FixProjectReferences(
        MoveOutcome outcome,
        ProjectId sourceId,
        ProjectId targetId,
        IReadOnlyList<ReferenceRequirement> required,
        IReadOnlyList<string> droppable)
    {
        var target = outcome.Solution.GetProject(targetId);
        if (target is null) return;

        foreach (var requirement in required)
        {
            var referenced = outcome.Solution.Projects.FirstOrDefault(p =>
                string.Equals(p.AssemblyName, requirement.Assembly, StringComparison.Ordinal));

            if (referenced is null || referenced.Id == targetId) continue;

            target = outcome.Solution.GetProject(targetId)!;
            if (target.ProjectReferences.Any(r => r.ProjectId == referenced.Id)) continue;

            // Why: Roslyn holds a cyclic project graph without complaint; MSBuild refuses to build it. So
            // a move that closes a loop looks clean in the tool and fails on the command line. Flag it
            // ahead of time with the chain that closes it, so it can be planned around.
            var cycle = ProjectReferenceCycle.DescribeCycle(outcome.Solution, targetId, referenced.Id);
            if (cycle is not null)
            {
                outcome.Cycles.Add(new BreakFinding
                {
                    Kind = "CircularReference",
                    FilePath = referenced.FilePath ?? string.Empty,
                    Severity = "High",
                    Detail = $"Referencing '{referenced.Name}' from '{target.Name}' would close a cycle: {cycle}",
                });
                continue;
            }

            outcome.Solution = outcome.Solution.AddProjectReference(targetId, new ProjectReference(referenced.Id));
            outcome.ReferencesToWrite.Add(referenced);
            outcome.PathChanges.Add(new PathChange(
                string.Empty,
                referenced.Name,
                "TargetProjectReferenceAdded"));
        }

        foreach (var assembly in droppable)
        {
            var referenced = outcome.Solution.Projects.FirstOrDefault(p =>
                string.Equals(p.AssemblyName, assembly, StringComparison.Ordinal));

            if (referenced is null) continue;

            var source = outcome.Solution.GetProject(sourceId);
            if (source is null || !source.ProjectReferences.Any(r => r.ProjectId == referenced.Id)) continue;

            outcome.Solution = outcome.Solution.RemoveProjectReference(sourceId, new ProjectReference(referenced.Id));
            outcome.PathChanges.Add(new PathChange(
                referenced.Name,
                string.Empty,
                "SourceProjectReferenceDropped"));
        }
    }

    private sealed class DocumentInProject
    {
        public DocumentInProject(Document document, Project project, IReadOnlyList<TypeDeclarationInfo> declarations)
        {
            Document = document;
            Project = project;
            Declarations = declarations;
        }

        public Document Document { get; }

        public Project Project { get; }

        public IReadOnlyList<TypeDeclarationInfo> Declarations { get; }
    }

    private sealed class MoveOutcome
    {
        public MoveOutcome(Solution solution) => Solution = solution;

        public Solution Solution { get; set; }

        public List<FileChange> ChangedFiles { get; } = new();

        public List<SymbolChange> SymbolChanges { get; } = new();

        public List<PathChange> PathChanges { get; } = new();

        public List<BreakFinding> Cycles { get; } = new();

        /// <summary>
        /// Gets the projects the target must reference, captured so they can be written to its csproj.
        /// </summary>
        /// <remarks>
        /// A ProjectReference is project METADATA, not document text, so adding it to the in-memory
        /// Solution is invisible to ApplyWorkspaceChanges — which writes documents. Without this the tool
        /// computed the whole closure, reported it, wired it in memory, and shipped a csproj that still
        /// had only its seed references: the answer existed solely in the command output.
        /// </remarks>
        public List<Project> ReferencesToWrite { get; } = new();
    }
}
