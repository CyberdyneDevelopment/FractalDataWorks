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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for <see cref="MoveTypesToNamespaceCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "MoveTypesToNamespace")]
public sealed class MoveTypesToNamespaceTranslator
    : RoslynCommandTranslatorBase<MoveTypesToNamespaceTranslator, MoveTypesToNamespaceCommand, IRoslynCommandResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveTypesToNamespaceTranslator"/> class.
    /// </summary>
    public MoveTypesToNamespaceTranslator()
        : base("MoveTypesToNamespace", "Re-homes selected types into the namespace they should declare")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear flow: select, rewrite declarations, follow references, probe.
    public override async Task<IGenericResult<IRoslynCommandResult>> Translate(
        MoveTypesToNamespaceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));

        MoveCommandLog.MoveStarting(Logger, "MoveTypesToNamespace",
            string.Join(", ", command.FilePaths), command.DryRun);

        if (string.IsNullOrWhiteSpace(command.NewNamespace))
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NamespaceRequired"));

        var generated = new List<string>();
        var selected = await SelectDocuments(solution, command, generated, cancellationToken).ConfigureAwait(false);

        MoveCommandLog.SelectionResolved(Logger, "MoveTypesToNamespace", selected.Count, generated.Count);

        if (generated.Count > 0)
            MoveCommandLog.SelectorMatchedGeneratedFile(Logger, generated[0]);

        if (generated.Count > 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("SelectorMatchedGeneratedFile"),
                ResultDetails.Create().With("FilePath", generated[0]));

        if (selected.Count == 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("NoTypesMatchedSelector"),
                ResultDetails.Create().With("Selector", string.Join(", ", command.FilePaths)));

        var moving = selected
            .Where(s => !string.Equals(s.Namespace, command.NewNamespace, StringComparison.Ordinal))
            .ToList();

        if (moving.Count == 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("TargetSameAsCurrent"),
                ResultDetails.Create().With("Namespace", command.NewNamespace));

        var outcome = await Rewrite(solution, moving, command, cancellationToken).ConfigureAwait(false);

        var movedTypeNames = moving.Select(m => m.TypeName).Distinct(StringComparer.Ordinal).ToList();

        var affectedProjectIds = ChangedProjectIds(outcome);
        var baseline = await DiagnosticDiff.Counts(
            affectedProjectIds.Select(solution.GetProject).Where(p => p is not null).Select(p => p!),
            cancellationToken).ConfigureAwait(false);

        var probed = await TypeCollisionProbe
            .Probe(outcome.Solution, affectedProjectIds, movedTypeNames, command, baseline, cancellationToken)
            .ConfigureAwait(false);


        var unverifiable = probed.Where(b => string.Equals(b.Kind, "ProbeUnavailable", StringComparison.Ordinal)).ToList();
        foreach (var u in unverifiable)
            MoveCommandLog.ProjectUnverifiable(Logger, u.FilePath);

        if (unverifiable.Count > 0 && command.AcceptUnverified)
            MoveCommandLog.ProceedingUnverified(Logger, "MoveTypesToNamespace", command.Reason ?? "(no reason given)");

        if (!command.DryRun && !command.AcceptUnverified && unverifiable.Count > 0)
        {
            MoveCommandLog.ChangeCannotBeVerified(Logger, "MoveTypesToNamespace", unverifiable[0].Detail);
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("ChangeCannotBeVerified"),
                ResultDetails.Create()
                    .With("ProjectCount", unverifiable.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("Detail", unverifiable[0].Detail));
        }

        var collisions = probed.Count(b => string.Equals(b.Kind, "TypeCollision", StringComparison.Ordinal));
        var unresolved = probed.Count(b => string.Equals(b.Kind, "UnresolvedReference", StringComparison.Ordinal));

        if (!command.DryRun && !command.AcceptUnverified && probed.Count > 0)
            return GenericResult<IRoslynCommandResult>.Failure(
                RoslynResultCodes.ByName("ChangeWouldNotCompile"),
                ResultDetails.Create()
                    .With("CollisionCount", collisions.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("UnresolvedCount", unresolved.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .With("First", probed[0].Detail));

        var data = BuildData(solution, command, moving, outcome, probed, collisions, unresolved);

        var summary =
            $"{(command.DryRun ? "[DryRun] " : string.Empty)}Re-homed {moving.Count} type(s) into " +
            $"'{command.NewNamespace}'; {outcome.ReferencesFollowed} reference(s) followed, " +
            $"{data.TypesLeftBehind} type(s) left in place, {probed.Count} problem(s)";

        MoveCommandLog.MoveComplete(Logger, "MoveTypesToNamespace",
            data.AffectedFiles.Count, data.ReferencesFollowed);

        if (command.DryRun)
            return GenericResult<IRoslynCommandResult>.Success(
                new QueryResult<MoveTypesToNamespaceData>(summary, data));

        return GenericResult<IRoslynCommandResult>.Success(
            new MutationResult<MoveTypesToNamespaceData>(
                summary,
                outcome.Solution,
                outcome.ChangedFiles,
                outcome.SymbolChanges,
                Array.Empty<PathChange>(),
                data));
    }
#pragma warning restore MA0051

    private static async Task<List<SelectedType>> SelectDocuments(
        Solution solution,
        MoveTypesToNamespaceCommand command,
        List<string> generated,
        CancellationToken cancellationToken)
    {
        var wanted = new HashSet<string>(
            (command.FilePaths ?? Array.Empty<string>()).Select(Normalise),
            StringComparer.OrdinalIgnoreCase);

        var results = new List<SelectedType>();
        if (wanted.Count == 0) return results;


        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document.FilePath is null || !wanted.Contains(Normalise(document.FilePath))) continue;

                if (command.IsGeneratedDocument(document))
                {
                    generated.Add(document.FilePath);
                    continue;
                }

                var declarations = await TypeDeclarationReader.Read(document, cancellationToken).ConfigureAwait(false);
                foreach (var declaration in declarations)
                {
                    if (string.IsNullOrEmpty(declaration.Namespace)) continue;
                    if (command.SkipTypes is not null &&
                        Array.Exists(command.SkipTypes, s => string.Equals(s, declaration.TypeName, StringComparison.Ordinal)))
                        continue;

                    results.Add(new SelectedType(document, project, declaration.Namespace, declaration.TypeName, declaration.IsTypeOption));
                }
            }
        }

        return results;
    }

    private static string Normalise(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Rewrites the selected declarations, then follows references to the types that moved.
    /// </summary>
    /// <remarks>
    /// The reference pass is PER TYPE, not per namespace — that distinction is the whole point. After
    /// re-homing <c>Foo</c> out of <c>A</c>, <c>A.Foo</c> must become <c>B.Foo</c> while <c>A.Bar</c>,
    /// which stayed, must be left exactly as it is. A namespace-wide text rewrite cannot tell them apart.
    /// </remarks>
    private static async Task<RehomeOutcome> Rewrite(
        Solution solution,
        IReadOnlyList<SelectedType> moving,
        MoveTypesToNamespaceCommand command,
        CancellationToken cancellationToken)
    {
        var outcome = new RehomeOutcome(solution);
        var movedDocumentIds = new HashSet<DocumentId>(moving.Select(m => m.Document.Id));

        foreach (var group in moving.GroupBy(m => m.Document.Id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RewriteDeclaration(outcome, group.First(), command.NewNamespace, cancellationToken).ConfigureAwait(false);
        }

        foreach (var moved in moving)
        {
            outcome.SymbolChanges.Add(new SymbolChange(
                moved.Namespace + "." + moved.TypeName,
                command.NewNamespace + "." + moved.TypeName,
                SymbolChangeTypes.Renamed.Name,
                "NamedType",
                moved.Document.FilePath,
                moved.Document.FilePath,
                moved.Project.AssemblyName,
                moved.Project.AssemblyName,
                NamespaceLayout.RelativePosition(moved.Project, moved.Document.FilePath)));
        }

        var emptied = await EmptiedNamespaces(
            outcome.Solution,
            moving.Select(m => m.Namespace).Distinct(StringComparer.Ordinal).ToList(),
            cancellationToken).ConfigureAwait(false);

        await FollowReferences(outcome, moving, command, movedDocumentIds, emptied, cancellationToken)
            .ConfigureAwait(false);

        return outcome;
    }

    /// <summary>
    /// Finds which of the moved-from namespaces no longer declare anything.
    /// </summary>
    /// <param name="solution">The solution AFTER the declarations were rewritten.</param>
    /// <param name="oldNamespaces">The namespaces the types were moved out of.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The namespaces that no declaration in the solution names any more.</returns>
    /// <remarks>
    /// Deliberately an EXISTENCE check, not a name-resolution one. CS0234 means "this namespace does not
    /// exist"; asking whether any declaration still names it answers precisely that, and nothing more —
    /// it does not try to model which files needed the import, which is where reimplementing C# lookup
    /// goes wrong. If the namespace also exists in referenced metadata the removal would be unnecessary
    /// rather than harmful, and the verification probe reports it either way.
    /// </remarks>
    private static async Task<HashSet<string>> EmptiedNamespaces(
        Solution solution,
        IReadOnlyList<string> oldNamespaces,
        CancellationToken cancellationToken)
    {
        var surviving = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is null) continue;

                foreach (var declaration in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
                {
                    var declared = declaration.Name.ToString();
                    foreach (var old in oldNamespaces)
                    {
                        if (IsSelfOrDescendant(declared, old)) surviving.Add(old);
                    }
                }
            }
        }

        return new HashSet<string>(oldNamespaces.Where(n => !surviving.Contains(n)), StringComparer.Ordinal);
    }

    private static async Task RewriteDeclaration(
        RehomeOutcome outcome,
        SelectedType selected,
        string newNamespace,
        CancellationToken cancellationToken)
    {
        var document = outcome.Solution.GetDocument(selected.Document.Id);
        if (document is null) return;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return;

        var replacement = SyntaxFactory.ParseName(newNamespace);
        var rewritten = root;

        foreach (var declaration in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
        {
            if (!string.Equals(declaration.Name.ToString(), selected.Namespace, StringComparison.Ordinal)) continue;

            rewritten = rewritten.ReplaceNode(
                rewritten.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>()
                    .First(d => string.Equals(d.Name.ToString(), selected.Namespace, StringComparison.Ordinal)).Name,
                replacement.WithTriviaFrom(declaration.Name));
            break;
        }

        if (ReferenceEquals(rewritten, root)) return;

        outcome.Solution = outcome.Solution.WithDocumentSyntaxRoot(document.Id, rewritten);
        outcome.DeclarationsChanged++;
        outcome.Record(document.FilePath ?? string.Empty, selected.Project.Name, 1);
    }

    private static async Task FollowReferences(
        RehomeOutcome outcome,
        IReadOnlyList<SelectedType> moving,
        MoveTypesToNamespaceCommand command,
        HashSet<DocumentId> movedDocumentIds,
        HashSet<string> emptiedNamespaces,
        CancellationToken cancellationToken)
    {
        var newNamespace = command.NewNamespace;

        // Qualified references to move: "OldNamespace.TypeName" -> "NewNamespace.TypeName", one entry per
        // moved type so a sibling that stayed behind is never caught by the same rewrite.
        var qualified = moving
            .ToDictionary(
                m => m.Namespace + "." + m.TypeName,
                m => newNamespace + "." + m.TypeName,
                StringComparer.Ordinal);

        var typeNames = new HashSet<string>(moving.Select(m => m.TypeName), StringComparer.Ordinal);
        var oldNamespaces = new HashSet<string>(moving.Select(m => m.Namespace), StringComparer.Ordinal);

        foreach (var projectId in outcome.Solution.ProjectIds)
        {
            var project = outcome.Solution.GetProject(projectId);
            if (project is null) continue;

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (command.IsGeneratedDocument(document)) continue;

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is null) continue;

                var rewritten = RewriteQualifiedNames(root, qualified, out var followed);
                rewritten = FixImports(rewritten, movedDocumentIds.Contains(document.Id), typeNames, oldNamespaces, emptiedNamespaces, newNamespace, ref followed);

                if (followed == 0) continue;

                outcome.Solution = outcome.Solution.WithDocumentSyntaxRoot(document.Id, rewritten);
                outcome.ReferencesFollowed += followed;
                outcome.Record(document.FilePath ?? string.Empty, project.Name, followed);
            }
        }
    }

    private static SyntaxNode RewriteQualifiedNames(
        SyntaxNode root,
        Dictionary<string, string> qualified,
        out int followed)
    {
        var matches = root.DescendantNodes(descendIntoTrivia: true)
            .OfType<NameSyntax>()
            .Where(n => qualified.ContainsKey(n.ToString()))
            .ToList();

        followed = matches.Count;
        if (matches.Count == 0) return root;

        return root.ReplaceNodes(
            matches,
            (original, _) => SyntaxFactory.ParseName(qualified[original.ToString()]).WithTriviaFrom(original));
    }

    /// <summary>
    /// Adds a using for the new namespace where a file referenced a moved type unqualified.
    /// </summary>
    /// <remarks>
    /// The type NAME is unchanged, so an unqualified reference resolves again as soon as the new namespace
    /// is imported. Only files that actually name a moved type get the using — adding it everywhere would
    /// leave unused imports. Note this repo does NOT enforce IDE0005/CS8019, so such an import would be
    /// silently untidy rather than a build failure — which is exactly why the check has to be deliberate
    /// here instead of relying on the compiler to catch it.
    /// </remarks>
    private static SyntaxNode FixImports(
        SyntaxNode root,
        bool isMovedDocument,
        HashSet<string> typeNames,
        HashSet<string> oldNamespaces,
        HashSet<string> emptiedNamespaces,
        string newNamespace,
        ref int followed)
    {
        if (root is not CompilationUnitSyntax unit) return root;

        var importsBefore = unit.Usings
            .Select(u => u.Name?.ToString())
            .Where(u => u is not null)
            .Select(u => u!)
            .ToList();

        if (emptiedNamespaces.Count > 0)
        {
            var dead = unit.Usings
                .Where(u => u.Name is not null && emptiedNamespaces.Contains(u.Name.ToString()))
                .ToList();

            if (dead.Count > 0)
            {
                // KeepLeadingTrivia so a file-header comment or #pragma above the first directive is not
                // deleted along with it — the same choice RemoveGlobalUsings makes.
                unit = (CompilationUnitSyntax)unit.RemoveNodes(dead, SyntaxRemoveOptions.KeepLeadingTrivia)!;
                followed += dead.Count;
            }
        }

        if (isMovedDocument) return unit;

        var namesUsed = unit.DescendantNodes(descendIntoTrivia: true)
            .OfType<SimpleNameSyntax>()
            .Any(i => typeNames.Contains(i.Identifier.ValueText));

        if (!namesUsed) return unit;

        if (importsBefore.Any(u => string.Equals(u, newNamespace, StringComparison.Ordinal))) return unit;

        var declared = unit.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString())
            .ToList();

        var resolvesToday =
            importsBefore.Any(oldNamespaces.Contains) ||
            declared.Any(d => oldNamespaces.Any(old => IsSelfOrDescendant(d, old)));

        if (!resolvesToday) return unit;

        if (declared.Any(d => IsSelfOrDescendant(d, newNamespace))) return unit;

        followed++;

        return unit.AddUsings(
            SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(newNamespace))
                .NormalizeWhitespace()
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed));
    }

    /// <summary>
    /// Determines whether <paramref name="candidate"/> is <paramref name="ancestor"/> or nested inside it.
    /// </summary>
    /// <param name="candidate">The namespace a file declares.</param>
    /// <param name="ancestor">The namespace whose types would be in scope.</param>
    /// <returns><see langword="true"/> when a type in the ancestor resolves without a using.</returns>
    /// <remarks>
    /// C# name lookup walks enclosing namespace scopes, so a file declaring A.B.C sees types in A.B and A
    /// without importing them. Matching on the dotted prefix rather than plain StartsWith avoids treating
    /// A.BC as nested inside A.B.
    /// </remarks>
    private static bool IsSelfOrDescendant(string candidate, string ancestor)
    {
        if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(ancestor)) return false;

        return string.Equals(candidate, ancestor, StringComparison.Ordinal)
            || candidate.StartsWith(ancestor + ".", StringComparison.Ordinal);
    }

    private static MoveTypesToNamespaceData BuildData(
        Solution original,
        MoveTypesToNamespaceCommand command,
        IReadOnlyList<SelectedType> moving,
        RehomeOutcome outcome,
        IReadOnlyList<BreakFinding> probed,
        int collisions,
        int unresolved)
    {
        var fromNamespaces = moving.Select(m => m.Namespace).Distinct(StringComparer.Ordinal).ToList();
        var typeOptionCount = moving.Count(m => m.IsTypeOption);

        return new MoveTypesToNamespaceData
        {
            NewNamespace = command.NewNamespace,
            MovedTypes = moving
                .Select(m => $"{m.Namespace}.{m.TypeName} -> {command.NewNamespace}.{m.TypeName}")
                .ToList(),
            FromNamespaces = fromNamespaces,
            DeclarationsChanged = outcome.DeclarationsChanged,
            ReferencesFollowed = outcome.ReferencesFollowed,
            TypesLeftBehind = CountLeftBehind(original, fromNamespaces, moving, command),
            TypeOptionIdsChanged = typeOptionCount,
            WasDryRun = command.DryRun,
            ConsumerImpact = typeOptionCount > 0
                ? $"CONSUMER-BREAKING for the {moving.Count} moved type(s): their fully-qualified name changes, and {typeOptionCount} carry a TypeOption whose FNV-1a Id changes with it. Types left in the old namespace are unaffected."
                : $"CONSUMER-BREAKING for the {moving.Count} moved type(s): their fully-qualified name changes. No TypeOption Ids affected. Types left in the old namespace are unaffected.",
            Breaks = probed,
            CollisionCount = collisions,
            UnresolvedCount = unresolved,
            AffectedFiles = outcome.ChangedFiles.Select(c => c.FilePath).Distinct(StringComparer.Ordinal).ToList(),
        };
    }

    private static int CountLeftBehind(
        Solution solution,
        IReadOnlyList<string> fromNamespaces,
        IReadOnlyList<SelectedType> moving,
        MoveTypesToNamespaceCommand command)
    {
        var movedKeys = new HashSet<string>(
            moving.Select(m => m.Namespace + "." + m.TypeName),
            StringComparer.Ordinal);

        var count = 0;
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath is null) continue;
                if (command.IsGeneratedPath(document.FilePath)) continue;

                var root = document.TryGetSyntaxRoot(out var syntaxRoot) ? syntaxRoot : null;
                if (root is null) continue;

                foreach (var declaration in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    if (declaration.Parent is BaseTypeDeclarationSyntax) continue;

                    var ns = NamespaceOf(declaration);
                    if (!fromNamespaces.Contains(ns, StringComparer.Ordinal)) continue;
                    if (movedKeys.Contains(ns + "." + declaration.Identifier.ValueText)) continue;

                    count++;
                }
            }
        }

        return count;
    }

    private static string NamespaceOf(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (current is BaseNamespaceDeclarationSyntax ns) return ns.Name.ToString();
        }

        return string.Empty;
    }

    private static List<ProjectId> ChangedProjectIds(RehomeOutcome outcome)
    {
        var names = new HashSet<string>(outcome.ChangedFiles.Select(c => c.ProjectName), StringComparer.Ordinal);

        return outcome.Solution.Projects
            .Where(p => names.Contains(p.Name))
            .Select(p => p.Id)
            .ToList();
    }

    private sealed class SelectedType
    {
        public SelectedType(Document document, Project project, string namespaceName, string typeName, bool isTypeOption)
        {
            Document = document;
            Project = project;
            Namespace = namespaceName;
            TypeName = typeName;
            IsTypeOption = isTypeOption;
        }

        public Document Document { get; }

        public Project Project { get; }

        public string Namespace { get; }

        public string TypeName { get; }

        public bool IsTypeOption { get; }
    }

    private sealed class RehomeOutcome
    {
        public RehomeOutcome(Solution solution) => Solution = solution;

        public Solution Solution { get; set; }

        public int DeclarationsChanged { get; set; }

        public int ReferencesFollowed { get; set; }

        public List<FileChange> ChangedFiles { get; } = new();

        public List<SymbolChange> SymbolChanges { get; } = new();

        public void Record(string filePath, string projectName, int changes) =>
            ChangedFiles.Add(new FileChange(filePath, FileChangeTypes.Modified, projectName) { TextChangeCount = changes });
    }
}
