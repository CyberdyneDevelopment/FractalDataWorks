#pragma warning disable CA1305 // Specify IFormatProvider - project commands use invariant strings

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Projects.Commands;
using Fdw.Roslyn.Commands.Projects.Helpers;
using Fdw.Roslyn.Commands.Projects.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translates a <see cref="MoveProjectsCommand"/> into computed path changes
/// for project directories, .csproj references, and .slnx entries.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "MoveProjectsTranslator")]
public sealed class MoveProjectsTranslator
    : RoslynCommandTranslatorBase<MoveProjectsCommand, MutationResult<MoveProjectsResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MoveProjectsTranslator"/> class.
    /// </summary>
    public MoveProjectsTranslator()
        : base("MoveProjectsTranslator", "Translates MoveProjectsCommand to compute project move operations")
    {
    }

    /// <inheritdoc />
    public override Task<IGenericResult<MutationResult<MoveProjectsResult>>> Translate(
        MoveProjectsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var validationFailure = Validate(command, solution);
        if (validationFailure != null)
        {
            return Task.FromResult(validationFailure);
        }

        var solutionDir = Path.GetDirectoryName(solution.FilePath)!;
        var sourceRoot = Path.Combine(solutionDir, "src");

        var projectsByName = BuildProjectLookup(solution);
        var currentDirs = BuildCurrentDirectoryMap(solution);
        var newDirs = ProjectPathComputer.BuildProjectDirectoryMap(command.Moves, currentDirs, sourceRoot);

        var movedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var move in command.Moves)
        {
            movedProjects.Add(move.ProjectName);
        }

        var csprojChanges = ComputeCsprojChanges(solution, currentDirs, newDirs, movedProjects);
        var projectMoves = ComputeProjectMoveDetails(command, projectsByName, currentDirs, newDirs, sourceRoot);
        var slnxChanges = ComputeSlnxChanges(command, projectsByName, solutionDir, solution.FilePath!);

        var result = new MoveProjectsResult(projectMoves, csprojChanges, slnxChanges);

        var summary = $"Computed moves for {projectMoves.Count} project(s): " +
                      $"{csprojChanges.Count} .csproj file(s) with reference changes, " +
                      $"{slnxChanges.ProjectPathChanges.Count} .slnx path change(s)";

        var pathChanges = ComputePathChanges(projectMoves, csprojChanges, slnxChanges);

        return Task.FromResult<IGenericResult<MutationResult<MoveProjectsResult>>>(
            GenericResult<MutationResult<MoveProjectsResult>>.Success(
                new MutationResult<MoveProjectsResult>(
                    summary,
                    solution,
                    Array.Empty<FileChange>(),
                    Array.Empty<SymbolChange>(),
                    pathChanges,
                    result)));
    }

    private static List<PathChange> ComputePathChanges(
        List<ProjectMoveDetail> projectMoves,
        List<CsprojChangeDetail> csprojChanges,
        SlnxChangeDetail slnxChanges)
    {
        var pathChanges = new List<PathChange>();

        foreach (var move in projectMoves)
        {
            pathChanges.Add(new PathChange(move.OriginalPath, move.NewPath, "Project"));
        }

        foreach (var csprojChange in csprojChanges)
        {
            foreach (var referenceChange in csprojChange.ReferenceChanges)
            {
                pathChanges.Add(new PathChange(referenceChange.OldInclude, referenceChange.NewInclude, "CsprojReference"));
            }
        }

        foreach (var slnxProjectChange in slnxChanges.ProjectPathChanges)
        {
            pathChanges.Add(new PathChange(slnxProjectChange.OldPath, slnxProjectChange.NewPath, "SlnxProject"));
        }

        return pathChanges;
    }

    private static IGenericResult<MutationResult<MoveProjectsResult>>? Validate(
        MoveProjectsCommand command,
        Solution solution)
    {
        if (command.Moves.Count == 0)
        {
            return GenericResult<MutationResult<MoveProjectsResult>>.Failure(
                RoslynResultCodes.ByName("NoMovesSpecified"));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var move in command.Moves)
        {
            if (!seen.Add(move.ProjectName))
            {
                return GenericResult<MutationResult<MoveProjectsResult>>.Failure(
                    RoslynResultCodes.ByName("DuplicateProjectInBatch"),
                    ResultDetails.Create("ProjectName", move.ProjectName));
            }
        }

        var solutionDir = Path.GetDirectoryName(solution.FilePath)!;
        var sourceRoot = Path.Combine(solutionDir, "src");
        var projectsByName = BuildProjectLookup(solution);

        foreach (var move in command.Moves)
        {
            if (!projectsByName.TryGetValue(move.ProjectName, out var project))
            {
                return GenericResult<MutationResult<MoveProjectsResult>>.Failure(
                    RoslynResultCodes.ByName("ProjectNotFound"),
                    ResultDetails.Create("ProjectName", move.ProjectName));
            }

            var currentSubfolder = ProjectPathComputer.GetCurrentSubfolder(project.FilePath!, sourceRoot);
            if (string.Equals(currentSubfolder, move.TargetFolder, StringComparison.OrdinalIgnoreCase))
            {
                return GenericResult<MutationResult<MoveProjectsResult>>.Failure(
                    RoslynResultCodes.ByName("TargetSameAsCurrent"),
                    ResultDetails.Create("ProjectName", move.ProjectName));
            }
        }

        return null;
    }

    private static Dictionary<string, Microsoft.CodeAnalysis.Project> BuildProjectLookup(Solution solution)
    {
        var lookup = new Dictionary<string, Microsoft.CodeAnalysis.Project>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in solution.Projects)
        {
            lookup[project.Name] = project;
        }

        return lookup;
    }

    private static Dictionary<string, string> BuildCurrentDirectoryMap(Solution solution)
    {
        var dirs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in solution.Projects)
        {
            if (project.FilePath != null)
            {
                dirs[project.Name] = Path.GetDirectoryName(project.FilePath)!;
            }
        }

        return dirs;
    }

    private static List<CsprojChangeDetail> ComputeCsprojChanges(
        Solution solution,
        Dictionary<string, string> currentDirs,
        Dictionary<string, string> newDirs,
        HashSet<string> movedProjects)
    {
        var csprojChanges = new List<CsprojChangeDetail>();

        foreach (var project in solution.Projects)
        {
            if (project.FilePath == null) continue;

            var referencingName = project.Name;
            var referencingMoved = movedProjects.Contains(referencingName);

            if (!newDirs.TryGetValue(referencingName, out var newReferencingDir)) continue;

            var referenceChanges = new List<ReferencePathChange>();

            foreach (var projectRef in project.ProjectReferences)
            {
                var referencedProject = solution.GetProject(projectRef.ProjectId);
                if (referencedProject?.FilePath == null) continue;

                var referencedName = referencedProject.Name;
                var referencedMoved = movedProjects.Contains(referencedName);

                if (!referencingMoved && !referencedMoved) continue;
                if (!newDirs.TryGetValue(referencedName, out var newReferencedDir)) continue;

                var csprojFileName = Path.GetFileName(referencedProject.FilePath);

                var oldRelative = ProjectPathComputer.ComputeNewRelativePath(
                    currentDirs[referencingName], currentDirs[referencedName], csprojFileName);

                var newRelative = ProjectPathComputer.ComputeNewRelativePath(
                    newReferencingDir, newReferencedDir, csprojFileName);

                if (!string.Equals(oldRelative, newRelative, StringComparison.OrdinalIgnoreCase))
                {
                    referenceChanges.Add(new ReferencePathChange(oldRelative, newRelative));
                }
            }

            if (referenceChanges.Count > 0)
            {
                csprojChanges.Add(new CsprojChangeDetail(project.FilePath, referenceChanges));
            }
        }

        return csprojChanges;
    }

    private static List<ProjectMoveDetail> ComputeProjectMoveDetails(
        MoveProjectsCommand command,
        Dictionary<string, Microsoft.CodeAnalysis.Project> projectsByName,
        Dictionary<string, string> currentDirs,
        Dictionary<string, string> newDirs,
        string sourceRoot)
    {
        var moves = new List<ProjectMoveDetail>();

        foreach (var move in command.Moves)
        {
            var originalPath = currentDirs[move.ProjectName];
            var newPath = newDirs[move.ProjectName];
            var originalFolder = ProjectPathComputer.GetCurrentSubfolder(
                projectsByName[move.ProjectName].FilePath!, sourceRoot);

            moves.Add(new ProjectMoveDetail(
                move.ProjectName, originalPath, newPath, originalFolder, move.TargetFolder));
        }

        return moves;
    }

    private static SlnxChangeDetail ComputeSlnxChanges(
        MoveProjectsCommand command,
        Dictionary<string, Microsoft.CodeAnalysis.Project> projectsByName,
        string solutionDir,
        string slnxPath)
    {
        var pathChanges = new List<SlnxProjectPathChange>();

        foreach (var move in command.Moves)
        {
            var project = projectsByName[move.ProjectName];
            if (project.FilePath == null) continue;

            var currentSlnxPath = Path.GetRelativePath(solutionDir, project.FilePath)
                .Replace(Path.DirectorySeparatorChar, '/');

            var newSlnxPath = ProjectPathComputer.ComputeNewSlnxPath(currentSlnxPath, move.TargetFolder);

            if (!string.Equals(currentSlnxPath, newSlnxPath, StringComparison.Ordinal))
            {
                pathChanges.Add(new SlnxProjectPathChange(currentSlnxPath, newSlnxPath));
            }
        }

        return new SlnxChangeDetail(slnxPath, pathChanges);
    }
}
