using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Helpers;
using Fdw.Roslyn.Commands.Analysis.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Analysis.Translators;

/// <summary>
/// Translator for <see cref="FindNamespaceMismatchesCommand"/>.
/// </summary>
/// <remarks>
/// Syntax-only by design. Building semantic models for a 444-project solution to answer a question that
/// is purely syntactic — what namespace is declared, where does the file sit, which project compiles it —
/// would make the command unusable at the scale it exists to serve.
/// </remarks>
[TypeOption(typeof(RoslynCommandTranslators), "FindNamespaceMismatches")]
public sealed class FindNamespaceMismatchesTranslator
    : RoslynCommandTranslatorBase<FindNamespaceMismatchesCommand, QueryResult<NamespaceMismatchReport>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindNamespaceMismatchesTranslator"/> class.
    /// </summary>
    public FindNamespaceMismatchesTranslator()
        : base("FindNamespaceMismatches", "Finds types whose namespace disagrees with their path or project")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<NamespaceMismatchReport>>> Translate(
        FindNamespaceMismatchesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
        {
            FindNamespaceMismatchesTranslatorLog.CommandCannotBeNull(Logger);
            return GenericResult<QueryResult<NamespaceMismatchReport>>.Failure(
                RoslynResultCodes.ByName("CommandCannotBeNull"));
        }

        FindNamespaceMismatchesTranslatorLog.Scanning(Logger, command.Scope ?? "(whole solution)", command.IncludeTests);

        var projects = SelectProjects(solution, command);
        var mismatches = new List<NamespaceMismatch>();
        var typesScanned = 0;

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(document.FilePath)) continue;
                if (command.IsGeneratedDocument(document)) continue;
                if (!InScope(command.Scope, project, document)) continue;

                var declarations = await TypeDeclarationReader
                    .Read(document, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var declaration in declarations)
                {
                    typesScanned++;
                    var mismatch = Evaluate(solution, project, document, declaration);
                    if (mismatch is not null) mismatches.Add(mismatch);
                }
            }
        }

        var kept = ApplyKindFilter(mismatches, command.IncludeKinds);

        if (kept.Count == 0)
        {
            FindNamespaceMismatchesTranslatorLog.NamespaceMismatchesNotFound(Logger, command.Scope ?? "(whole solution)");
            return GenericResult<QueryResult<NamespaceMismatchReport>>.Failure(
                RoslynResultCodes.ByName("NamespaceMismatchesNotFound"),
                ResultDetails.Create().With("Scope", command.Scope ?? "(whole solution)"));
        }

        var report = BuildReport(kept, typesScanned, command.IncludeTests, command.IncludeTypes, command.MaxTypesPerGroup);

        FindNamespaceMismatchesTranslatorLog.Completed(Logger, typesScanned, report.TotalMismatches, report.GroupCount);

        return GenericResult<QueryResult<NamespaceMismatchReport>>.Success(
            new QueryResult<NamespaceMismatchReport>(
                $"{report.TotalMismatches} mismatched type(s) in {report.GroupCount} group(s); {report.TypeOptionCount} carry a TypeOption",
                report));
    }

    private static List<Project> SelectProjects(Solution solution, FindNamespaceMismatchesCommand command)
    {
        var projects = solution.Projects.AsEnumerable();
        if (!command.IncludeTests)
            projects = projects.Where(p => !TestProjectDetector.IsTestProject(p.Name));
        return projects.ToList();
    }

    private static bool InScope(string? scope, Project project, Document document)
    {
        if (string.IsNullOrWhiteSpace(scope)) return true;

        return project.Name.Contains(scope!, StringComparison.OrdinalIgnoreCase)
            || (document.FilePath?.Contains(scope!, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    // Why: the two kinds are mutually exclusive by construction. If the current project does not own the
    // namespace the project is wrong, and judging the folder separately would report a path "error" for a
    // file that is about to move anyway. "Both" is therefore a FILTER value meaning "include both kinds",
    // never an emitted finding — which is what makes the default (include everything) non-narrowing.
    private static NamespaceMismatch? Evaluate(
        Solution solution,
        Project project,
        Document document,
        TypeDeclarationInfo declaration)
    {
        if (string.IsNullOrEmpty(declaration.Namespace)) return null;

        var currentProjectOwns = NamespaceLayout.RelativeFolders(declaration.Namespace, project.Name) is not null;

        if (!currentProjectOwns)
        {
            var exact = ExactProject(solution, declaration.Namespace);
            var nearest = LongestOwningProject(solution, declaration.Namespace);

            return new NamespaceMismatch
            {
                FullName = declaration.Namespace + "." + declaration.TypeName,
                Namespace = declaration.Namespace,
                CurrentPath = document.FilePath!,
                // Only a project of the RIGHT NAME yields a destination path. Deriving one from the
                // nearest ancestor would propose folding a backend vocabulary into a generic package.
                ExpectedPath = exact is null
                    ? null
                    : NamespaceLayout.ExpectedPath(exact, declaration.Namespace, declaration.TypeName),
                CurrentProject = project.Name,
                ExpectedProject = exact?.Name,
                NearestOwningProject = nearest?.Name,
                ExpectedProjectExists = exact is not null,
                MismatchKind = MismatchKinds.Project.Name,
                IsTypeOption = declaration.IsTypeOption,
            };
        }

        var expectedPath = NamespaceLayout.ExpectedPath(project, declaration.Namespace, declaration.TypeName);
        if (expectedPath is null || NamespaceLayout.SamePath(expectedPath, document.FilePath))
            return null;

        return new NamespaceMismatch
        {
            FullName = declaration.Namespace + "." + declaration.TypeName,
            Namespace = declaration.Namespace,
            CurrentPath = document.FilePath!,
            ExpectedPath = expectedPath,
            CurrentProject = project.Name,
            ExpectedProject = project.Name,
            NearestOwningProject = project.Name,
            ExpectedProjectExists = true,
            MismatchKind = MismatchKinds.Path.Name,
            IsTypeOption = declaration.IsTypeOption,
        };
    }

    private static Project? ExactProject(Solution solution, string namespaceName) =>
        solution.Projects.FirstOrDefault(p => string.Equals(p.Name, namespaceName, StringComparison.Ordinal));

    /// <summary>
    /// Finds the project that owns a namespace — the longest project name the namespace sits under.
    /// </summary>
    /// <remarks>
    /// Returns null when no project owns the namespace at all, which is a finding rather than an error:
    /// those types have no home and the split needs a new package for them.
    /// </remarks>
    private static Project? LongestOwningProject(Solution solution, string namespaceName)
    {
        Project? best = null;
        foreach (var project in solution.Projects)
        {
            if (NamespaceLayout.RelativeFolders(namespaceName, project.Name) is null) continue;
            if (best is null || project.Name.Length > best.Name.Length) best = project;
        }

        return best;
    }

    private static IReadOnlyList<NamespaceMismatch> ApplyKindFilter(
        IReadOnlyList<NamespaceMismatch> mismatches,
        string[] includeKinds)
    {
        if (includeKinds is null || includeKinds.Length == 0) return mismatches;

        var wanted = includeKinds
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Replace("Mismatch", string.Empty, StringComparison.OrdinalIgnoreCase).Trim())
            .ToList();

        if (wanted.Count == 0) return mismatches;
        if (wanted.Exists(k => string.Equals(k, MismatchKinds.Both.Name, StringComparison.OrdinalIgnoreCase)))
            return mismatches;

        return mismatches
            .Where(m => wanted.Exists(k => string.Equals(k, m.MismatchKind, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static NamespaceMismatchReport BuildReport(
        IReadOnlyList<NamespaceMismatch> mismatches,
        int typesScanned,
        bool includedTests,
        bool includeTypes,
        int maxTypesPerGroup)
    {
        var groups = mismatches
            .GroupBy(m => new
            {
                m.Namespace,
                m.CurrentProject,
                m.ExpectedProject,
                m.MismatchKind,
            })
            .Select(g => BuildGroup(g.Key.Namespace, g.Key.CurrentProject, g.Key.ExpectedProject, g.Key.MismatchKind, g.ToList(), includeTypes, maxTypesPerGroup))
            .OrderByDescending(g => g.TypeCount)
            .ThenBy(g => g.Namespace, StringComparer.Ordinal)
            .ToList();

        return new NamespaceMismatchReport
        {
            TotalMismatches = mismatches.Count,
            TypesScanned = typesScanned,
            GroupCount = groups.Count,
            TypeOptionCount = mismatches.Count(m => m.IsTypeOption),
            GroupsWithoutTargetProject = groups.Count(g => !g.ExpectedProjectExists),
            IncludedTests = includedTests,
            Groups = groups,
        };
    }

    // Why: "move it to the project its namespace names" is only actionable when such a project EXISTS.
    // When it does not, silently pointing at the nearest ancestor project would send the caller to a
    // destination nobody chose. The honest answer is to name the two real options — rename the namespace
    // to match where the types already live, or create the project — and say which types are affected.
    private static NamespaceMismatchGroup BuildGroup(
        string namespaceName,
        string currentProject,
        string? expectedProject,
        string mismatchKind,
        List<NamespaceMismatch> types,
        bool includeTypes,
        int maxTypesPerGroup)
    {
        var exists = types.Count > 0 && types[0].ExpectedProjectExists;
        var nearest = types.Count > 0 ? types[0].NearestOwningProject : null;

        var carried = includeTypes
            ? types.OrderBy(m => m.FullName, StringComparer.Ordinal)
                   .Take(Math.Max(0, maxTypesPerGroup))
                   .ToList()
            : new List<NamespaceMismatch>();

        return new NamespaceMismatchGroup
        {
            Namespace = namespaceName,
            CurrentProject = currentProject,
            ExpectedProject = expectedProject,
            NearestOwningProject = nearest,
            ExpectedProjectExists = exists,
            MismatchKind = mismatchKind,
            SuggestedAction = exists ? "MoveTypeToProject" : "CreateProject or MoveNamespace",
            Notice = exists
                ? null
                : BuildNoProjectNotice(namespaceName, currentProject, nearest, types.Count),
            TypeCount = types.Count,
            TypeOptionCount = types.Count(m => m.IsTypeOption),
            TypesOmitted = types.Count - carried.Count,
            Types = carried,
        };
    }

    private static string BuildNoProjectNotice(
        string namespaceName,
        string currentProject,
        string? expectedProject,
        int typeCount)
    {
        var nearest = expectedProject is null
            ? "No project owns this namespace at any level."
            : $"The nearest owning project is '{expectedProject}', which would place them in a sub-folder of it rather than a project of their own.";

        return $"No project is named '{namespaceName}', so these {typeCount} type(s) cannot simply be moved into one. {nearest} " +
               $"Either create project '{namespaceName}' and move them there, or use MoveNamespace to rename '{namespaceName}' to a namespace that matches where they already live (currently '{currentProject}'). " +
               "Note MoveNamespace is consumer-breaking and changes any TypeOption Id derived from the FQN, whereas creating the project and moving is not.";
    }
}
