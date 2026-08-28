using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// Provides in-memory Roslyn Solution management with snapshot/rollback capabilities
/// and project filtering for resource optimization.
/// </summary>
/// <remarks>
/// <para>
/// This class wraps a Roslyn Solution and provides snapshot functionality for
/// tracking changes and supporting rollback operations. It is thread-safe for
/// concurrent access.
/// </para>
/// <para>
/// Remember: Roslyn Solutions are immutable. Every modification operation returns
/// a new Solution instance. Always capture the result and call <see cref="UpdateSolution"/>
/// to apply changes.
/// </para>
/// <para>
/// Project filtering allows excluding test projects and other non-essential projects
/// from the workspace to reduce memory usage. Excluded projects can be loaded on-demand.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class RoslynWorkspace : IRoslynWorkspace
{
    private Solution _currentSolution;
    private Solution? _baselineSolution;
    private Solution _lastApplied;

    /// <summary>
    /// Gets the MSBuild failures reported while the solution was loading.
    /// </summary>
    /// <remarks>
    /// A project MSBuild could not evaluate still loads — with zero metadata references — so every later
    /// compilation of it fails to resolve System and reports the whole BCL as missing. That symptom is
    /// hundreds of diagnostics away from this cause, which is why the cause is kept rather than logged
    /// into a NullLogger and lost.
    /// </remarks>
    public IReadOnlyList<string> LoadDiagnostics { get; }
    private readonly Solution _fullSolution;
    private readonly string _solutionPath;
    private readonly ConcurrentDictionary<string, WorkspaceSnapshot> _snapshots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SolutionProjectInfo> _allProjects = new(StringComparer.OrdinalIgnoreCase);
    private List<string> _excludePatterns = [];

    /// <summary>
    /// Internal tracking for projects from the solution file.
    /// </summary>
    private sealed class SolutionProjectInfo
    {
        public required string Name { get; init; }
        public required string FilePath { get; init; }
        public required ProjectId? LoadedProjectId { get; set; }
        public bool IsLoaded => LoadedProjectId is not null;
        public bool IsExcluded { get; set; }
        public string? ExcludedByPattern { get; set; }
        public string? Language { get; set; }
        public List<string> ProjectReferences { get; init; } = [];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspace"/> class.
    /// </summary>
    /// <param name="initialSolution">The initial solution to manage.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialSolution"/> is null.</exception>
    public RoslynWorkspace(Solution initialSolution)
        : this(initialSolution, initialSolution, initialSolution.FilePath ?? string.Empty, [], null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RoslynWorkspace"/> class with project filtering.
    /// </summary>
    /// <param name="filteredSolution">The solution with excluded projects removed.</param>
    /// <param name="fullSolution">The full solution with all projects.</param>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="excludePatterns">The patterns used to exclude projects.</param>
    /// <param name="loadDiagnostics">The MSBuild failures reported while loading, if any.</param>
    internal RoslynWorkspace(
        Solution filteredSolution,
        Solution fullSolution,
        string solutionPath,
        IReadOnlyList<string> excludePatterns,
        IReadOnlyList<string>? loadDiagnostics = null)
    {
        LoadDiagnostics = loadDiagnostics ?? [];
        _currentSolution = filteredSolution ?? throw new ArgumentNullException(nameof(filteredSolution));
        _fullSolution = fullSolution ?? throw new ArgumentNullException(nameof(fullSolution));
        _baselineSolution = filteredSolution;
        _lastApplied = filteredSolution;
        _solutionPath = solutionPath;
        _excludePatterns = excludePatterns?.ToList() ?? [];

        // Build project tracking from full solution
        InitializeProjectTracking();
    }

    private void InitializeProjectTracking()
    {
        _allProjects.Clear();

        // Track all projects from the full solution
        foreach (var project in _fullSolution.Projects)
        {
            if (project.FilePath is null) continue;

            var info = new SolutionProjectInfo
            {
                Name = project.Name,
                FilePath = project.FilePath,
                LoadedProjectId = null,
                Language = project.Language,
                ProjectReferences = project.ProjectReferences
                    .Select(r => _fullSolution.GetProject(r.ProjectId)?.Name)
                    .Where(n => n is not null)
                    .Cast<string>()
                    .ToList()
            };

            // Check if excluded by pattern
            foreach (var pattern in _excludePatterns)
            {
                if (MatchesPattern(project.Name, pattern))
                {
                    info.IsExcluded = true;
                    info.ExcludedByPattern = pattern;
                    break;
                }
            }

            _allProjects[project.Name] = info;
        }

        // Mark loaded projects
        foreach (var project in _currentSolution.Projects)
        {
            if (_allProjects.TryGetValue(project.Name, out var info))
            {
                info.LoadedProjectId = project.Id;
                info.IsExcluded = false;
                info.ExcludedByPattern = null;
            }
        }
    }

    /// <inheritdoc/>
    public Solution Current => _currentSolution;

    /// <inheritdoc/>
    public Solution? Baseline => _baselineSolution;

    /// <inheritdoc/>
    public Solution CurrentSolution => _currentSolution;

    /// <inheritdoc/>
    public Solution? BaselineSolution => _baselineSolution;

    /// <inheritdoc/>
    public int SnapshotCount => _snapshots.Count;

    /// <inheritdoc/>
    public IReadOnlyList<string> ExcludePatterns => _excludePatterns.AsReadOnly();

    /// <inheritdoc/>
    public bool HasChanges
    {
        get
        {
            if (_baselineSolution is null)
                return false;

            // Quick check: if they're the same reference, no changes
            if (ReferenceEquals(_currentSolution, _baselineSolution))
                return false;

            // Check if any documents differ
            return GetChangesFromBaseline().Count > 0;
        }
    }

    /// <inheritdoc/>
    public void Update(Solution state)
    {
        _currentSolution = state ?? throw new ArgumentNullException(nameof(state));
    }

    /// <inheritdoc/>
    public void UpdateSolution(Solution solution)
    {
        Update(solution);
    }

    /// <inheritdoc/>
    public void SetBaseline(Solution state)
    {
        _baselineSolution = state;
    }

    /// <inheritdoc/>
    public string CreateSnapshot(string name, string description)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        var snapshotId = Guid.NewGuid().ToString("N");
        var snapshot = new WorkspaceSnapshot(
            snapshotId,
            name,
            description ?? string.Empty,
            _currentSolution,
            DateTime.UtcNow);

        _snapshots[snapshotId] = snapshot;
        return snapshotId;
    }

    /// <inheritdoc/>
    public IGenericResult<Solution> RestoreSnapshot(string snapshotId)
    {
        if (string.IsNullOrEmpty(snapshotId))
            return GenericResult<Solution>.Failure(WorkspaceResultCodes.ByName("SnapshotIdRequired"));

        if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
            return GenericResult<Solution>.Failure(
                WorkspaceResultCodes.ByName("SnapshotNotFound"),
                ResultDetails.Create("SnapshotId", snapshotId));

        _currentSolution = snapshot.Solution;
        return GenericResult<Solution>.Success(_currentSolution);
    }

    /// <inheritdoc/>
    public IEnumerable<SnapshotInfo> ListSnapshots()
    {
        return _snapshots.Values
            .Select(s => new SnapshotInfo(s.Id, s.Name, s.Description, s.CreatedAt))
            .OrderByDescending(s => s.CreatedAt);
    }

    /// <inheritdoc/>
    public bool RemoveSnapshot(string snapshotId)
    {
        return _snapshots.TryRemove(snapshotId, out _);
    }

    /// <inheritdoc/>
    public void ClearSnapshots()
    {
        _snapshots.Clear();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetChangesFromBaseline()
    {
        if (_baselineSolution is null)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        return GetChangesBetween(_baselineSolution, _currentSolution);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string>? GetChangesFromSnapshot(string snapshotId)
    {
        if (string.IsNullOrEmpty(snapshotId))
            return null;

        if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
            return null;

        return GetChangesBetween(snapshot.Solution, _currentSolution);
    }

    /// <inheritdoc/>
    public void ApplyDocumentChanges(IReadOnlyDictionary<string, string> documentChanges)
    {
        if (documentChanges is null || documentChanges.Count == 0)
            return;

        var solution = _currentSolution;

        // Build a lookup of file paths to document IDs
        var pathToDocId = new Dictionary<string, DocumentId>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath is not null)
                {
                    pathToDocId[document.FilePath] = document.Id;
                }
            }
        }

        // Apply each change
        foreach (var (filePath, content) in documentChanges)
        {
            if (pathToDocId.TryGetValue(filePath, out var docId))
            {
                var sourceText = SourceText.From(content);
                solution = solution.WithDocumentText(docId, sourceText);
            }
        }

        _currentSolution = solution;
    }

#pragma warning disable MA0051 // Linear file-write loop; reads explanatory at every branch
    /// <summary>
    /// Deletes files whose documents disappeared from the solution since the last apply.
    /// </summary>
    /// <remarks>
    /// This is what makes a MOVE actually a move. Without it the source file survives alongside the new
    /// one and the type is declared twice, which is a duplicate-type build break rather than a refactor.
    ///
    /// Two guards, because deleting is irreversible: a path that some current document still occupies is
    /// never touched (that is a rename within the solution, not a removal), and a file whose on-disk text
    /// no longer matches what the workspace last saw is left alone and reported — someone changed it
    /// outside this session and their edit is not ours to discard.
    /// </remarks>
    private async Task DeleteRemovedDocuments(
        List<string> written,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        foreach (var oldProject in _lastApplied.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var newProject = _currentSolution.GetProject(oldProject.Id);

            foreach (var oldDocument in oldProject.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrEmpty(oldDocument.FilePath)) continue;
                if (newProject?.GetDocument(oldDocument.Id) is not null) continue;
                if (_currentSolution.GetDocumentIdsWithFilePath(oldDocument.FilePath).Length > 0) continue;
                if (!File.Exists(oldDocument.FilePath)) continue;

                try
                {
                    var lastKnown = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var onDisk = await File.ReadAllTextAsync(oldDocument.FilePath, cancellationToken)
                        .ConfigureAwait(false);

                    if (!string.Equals(onDisk, lastKnown.ToString(), StringComparison.Ordinal))
                    {
                        errors.Add($"{oldDocument.FilePath}: changed on disk since it was loaded; not deleted");
                        continue;
                    }

                    File.Delete(oldDocument.FilePath);
                    written.Add(oldDocument.FilePath);
                }
#pragma warning disable FDW014 // Per-file errors are aggregated into the final GenericResult.Failure detail "Errors"
                catch (IOException ex)
                {
                    errors.Add($"{oldDocument.FilePath}: {ex.GetType().Name}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add($"{oldDocument.FilePath}: {ex.GetType().Name}: {ex.Message}");
                }
#pragma warning restore FDW014
            }
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default) =>
        ApplyChanges(deleteRemovedFiles: false, cancellationToken);

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(
        bool deleteRemovedFiles,
        CancellationToken cancellationToken = default)
    {
        var written = new List<string>();
        var errors = new List<string>();

        foreach (var newProject in _currentSolution.Projects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var oldProject = _lastApplied.GetProject(newProject.Id);

            foreach (var newDocument in newProject.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrEmpty(newDocument.FilePath))
                    continue;

                var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                if (oldProject?.GetDocument(newDocument.Id) is { } oldDocument)
                {
                    var oldText = await oldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    if (newText.ContentEquals(oldText))
                        continue;
                }

                try
                {
                    var targetDirectory = Path.GetDirectoryName(newDocument.FilePath);
                    if (!string.IsNullOrEmpty(targetDirectory))
                        Directory.CreateDirectory(targetDirectory!);

                    var encoding = newText.Encoding ?? new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    await File.WriteAllTextAsync(newDocument.FilePath, newText.ToString(), encoding, cancellationToken).ConfigureAwait(false);
                    written.Add(newDocument.FilePath);
                }
#pragma warning disable FDW014 // Per-file errors are aggregated into the final GenericResult.Failure detail "Errors"
                catch (IOException ex)
                {
                    errors.Add($"{newDocument.FilePath}: {ex.GetType().Name}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    errors.Add($"{newDocument.FilePath}: {ex.GetType().Name}: {ex.Message}");
                }
#pragma warning restore FDW014
            }
        }

        if (deleteRemovedFiles)
            await DeleteRemovedDocuments(written, errors, cancellationToken).ConfigureAwait(false);

        if (errors.Count > 0)
        {
            return GenericResult<IReadOnlyList<string>>.Failure(
                WorkspaceResultCodes.ByName("ApplyChangesFailed"),
                ResultDetails.Create()
                    .With("WrittenCount", written.Count)
                    .With("ErrorCount", errors.Count)
                    .With("Errors", string.Join(" || ", errors)));
        }

        _lastApplied = _currentSolution;

        return GenericResult<IReadOnlyList<string>>.Success(written);
    }
#pragma warning restore MA0051

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetAllProjects()
    {
        return _allProjects.Values
            .Select(ToProjectInfo)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetLoadedProjects()
    {
        return _allProjects.Values
            .Where(p => p.IsLoaded)
            .Select(ToProjectInfo)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ProjectInfo> GetExcludedProjects()
    {
        return _allProjects.Values
            .Where(p => !p.IsLoaded)
            .Select(ToProjectInfo)
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear project loading: validate, resolve from full solution, add documents and references
    public async Task<IGenericResult<ProjectInfo>> LoadProject(
        string projectName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(projectName))
            return GenericResult<ProjectInfo>.Failure(WorkspaceResultCodes.ByName("ProjectNameRequired"));

        if (!_allProjects.TryGetValue(projectName, out var projectInfo))
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectNotFoundInSolution"),
                ResultDetails.Create("ProjectName", projectName));

        if (projectInfo.IsLoaded)
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectAlreadyLoaded"),
                ResultDetails.Create("ProjectName", projectName));

        try
        {
            // Get the project from the full solution
            var fullProject = _fullSolution.Projects.FirstOrDefault(p =>
                string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));

            if (fullProject is null)
                return GenericResult<ProjectInfo>.Failure(
                    WorkspaceResultCodes.ByName("ProjectNotFoundInFullSolution"),
                    ResultDetails.Create("ProjectName", projectName));

            // Add the project to the current solution
            // We need to use MSBuildWorkspace to properly load the project with its references
            using var msbuildWorkspace = MSBuildWorkspace.Create();
            var loadedProject = await msbuildWorkspace.OpenProjectAsync(
                projectInfo.FilePath,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var projectId = ProjectId.CreateNewId();
            var newSolution = _currentSolution.AddProject(
                Microsoft.CodeAnalysis.ProjectInfo.Create(
                    projectId,
                    VersionStamp.Create(),
                    loadedProject.Name,
                    loadedProject.AssemblyName,
                    loadedProject.Language,
                    filePath: loadedProject.FilePath)
                .WithCompilationOptions(loadedProject.CompilationOptions)
                .WithParseOptions(loadedProject.ParseOptions)
                .WithMetadataReferences(loadedProject.MetadataReferences));

            // Add documents from the loaded project
            foreach (var doc in loadedProject.Documents)
            {
                if (doc.FilePath is not null)
                {
                    var text = await doc.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    newSolution = newSolution.AddDocument(
                        DocumentId.CreateNewId(projectId),
                        doc.Name,
                        text,
                        filePath: doc.FilePath);
                }
            }

            // Add project references to other loaded projects
            foreach (var projRef in fullProject.ProjectReferences)
            {
                var refProject = _fullSolution.GetProject(projRef.ProjectId);
                if (refProject is not null)
                {
                    var loadedRef = _currentSolution.Projects.FirstOrDefault(p =>
                        string.Equals(p.Name, refProject.Name, StringComparison.OrdinalIgnoreCase));
                    if (loadedRef is not null)
                    {
                        newSolution = newSolution.AddProjectReference(
                            projectId,
                            new ProjectReference(loadedRef.Id));
                    }
                }
            }

            _currentSolution = newSolution;

            // Update tracking
            projectInfo.LoadedProjectId = projectId;
            projectInfo.IsExcluded = false;
            projectInfo.ExcludedByPattern = null;

            return GenericResult<ProjectInfo>.Success(ToProjectInfo(projectInfo));
        }
        catch (Exception ex)
        {
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectLoadFailed"),
                ResultDetails.Create("ProjectName", projectName, "ErrorMessage", ex.Message));
        }
    }
#pragma warning restore MA0051

    /// <inheritdoc/>
    public IGenericResult<ProjectInfo> UnloadProject(string projectName, bool force = false)
    {
        if (string.IsNullOrEmpty(projectName))
            return GenericResult<ProjectInfo>.Failure(WorkspaceResultCodes.ByName("ProjectNameRequired"));

        if (!_allProjects.TryGetValue(projectName, out var projectInfo))
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectNotFoundInSolution"),
                ResultDetails.Create("ProjectName", projectName));

        if (!projectInfo.IsLoaded)
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectNotLoaded"),
                ResultDetails.Create("ProjectName", projectName));

        // Check for pending changes
        if (!force && HasPendingChanges(projectName))
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectHasPendingChanges"),
                ResultDetails.Create("ProjectName", projectName));

        // Check if other loaded projects depend on this one
        var dependents = _allProjects.Values
            .Where(p => p.IsLoaded && p.ProjectReferences.Contains(projectName, StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();

        if (dependents.Count > 0)
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectHasDependents"),
                ResultDetails.Create("ProjectName", projectName, "Dependents", string.Join(", ", dependents)));

        // Remove the project from the solution
        var project = _currentSolution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
            return GenericResult<ProjectInfo>.Failure(
                WorkspaceResultCodes.ByName("ProjectNotFoundInCurrentSolution"),
                ResultDetails.Create("ProjectName", projectName));

        _currentSolution = _currentSolution.RemoveProject(project.Id);

        // Update tracking
        projectInfo.LoadedProjectId = null;
        projectInfo.IsExcluded = true;

        // Check if it matches any exclude pattern
        foreach (var pattern in _excludePatterns)
        {
            if (MatchesPattern(projectName, pattern))
            {
                projectInfo.ExcludedByPattern = pattern;
                break;
            }
        }

        return GenericResult<ProjectInfo>.Success(ToProjectInfo(projectInfo));
    }

    /// <inheritdoc/>
    public bool HasPendingChanges(string projectName)
    {
        if (!_allProjects.TryGetValue(projectName, out var projectInfo))
            return false;

        if (!projectInfo.IsLoaded || projectInfo.LoadedProjectId is null)
            return false;

        if (_baselineSolution is null)
            return false;

        var currentProject = _currentSolution.GetProject(projectInfo.LoadedProjectId);
        if (currentProject is null)
            return false;

        // Check if any documents in this project have changed
        foreach (var doc in currentProject.Documents)
        {
            if (doc.FilePath is null) continue;

            var currentText = GetDocumentText(doc);
            if (currentText is null) continue;

            // Find the same document in baseline
            var baselineDoc = _baselineSolution.Projects
                .SelectMany(p => p.Documents)
                .FirstOrDefault(d => string.Equals(d.FilePath, doc.FilePath, StringComparison.OrdinalIgnoreCase));

            if (baselineDoc is null)
            {
                // New document
                return true;
            }

            var baselineText = GetDocumentText(baselineDoc);
            if (!string.Equals(baselineText, currentText, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void SetExcludePatterns(IReadOnlyList<string> patterns)
    {
        _excludePatterns = patterns?.ToList() ?? [];

        // Update excluded status for all unloaded projects
        foreach (var info in _allProjects.Values.Where(p => !p.IsLoaded))
        {
            info.IsExcluded = false;
            info.ExcludedByPattern = null;

            foreach (var pattern in _excludePatterns)
            {
                if (MatchesPattern(info.Name, pattern))
                {
                    info.IsExcluded = true;
                    info.ExcludedByPattern = pattern;
                    break;
                }
            }
        }
    }

    private ProjectInfo ToProjectInfo(SolutionProjectInfo info)
    {
        // Calculate who references this project
        var referencedBy = _allProjects.Values
            .Where(p => p.ProjectReferences.Contains(info.Name, StringComparer.OrdinalIgnoreCase))
            .Select(p => p.Name)
            .ToList();

        return new ProjectInfo
        {
            Name = info.Name,
            FilePath = info.FilePath,
            IsLoaded = info.IsLoaded,
            IsExcluded = info.IsExcluded,
            ExcludedByPattern = info.ExcludedByPattern,
            IsTestProject = IsTestProjectName(info.Name),
            Language = info.Language,
            ProjectReferences = info.ProjectReferences,
            ReferencedBy = referencedBy
        };
    }

    private static bool IsTestProjectName(string name)
    {
        return name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".UnitTests", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".IntegrationTests", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".Test", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".Specs", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith(".Benchmarks", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesPattern(string name, string pattern)
    {
        // Convert glob pattern to regex
        // Supports: * (any chars), ? (single char)
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(name, regexPattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    private static Dictionary<string, string> GetChangesBetween(Solution from, Solution to)
    {
        var changes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Build lookup of baseline documents by file path
        var baselineDocuments = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in from.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath is not null)
                {
                    baselineDocuments[document.FilePath] = document;
                }
            }
        }

        // Check each current document against baseline
        foreach (var project in to.Projects)
        {
            foreach (var document in project.Documents)
            {
                if (document.FilePath is null)
                    continue;

                var currentText = GetDocumentText(document);
                if (currentText is null)
                    continue;

                if (baselineDocuments.TryGetValue(document.FilePath, out var baselineDoc))
                {
                    var baselineText = GetDocumentText(baselineDoc);
                    if (!string.Equals(baselineText, currentText, StringComparison.Ordinal))
                    {
                        changes[document.FilePath] = currentText;
                    }
                }
                else
                {
                    // New document not in baseline
                    changes[document.FilePath] = currentText;
                }
            }
        }

        return changes;
    }

    private static string? GetDocumentText(Document document)
    {
        // Use synchronous API - TryGetText returns cached text if available
        // If text isn't cached, we load it synchronously (acceptable for workspace operations)
        if (!document.TryGetText(out var sourceText))
        {
            // Text not cached - use Task.Run to avoid deadlock on UI thread contexts
#pragma warning disable VSTHRD002 // Synchronous wait is acceptable here as we're in a background context
            sourceText = document.GetTextAsync(CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        return sourceText?.ToString();
    }
}
