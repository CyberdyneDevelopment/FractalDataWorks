using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.Dac.Model;

namespace Fdw.Sql.Workspace;

/// <summary>
/// Default <see cref="ISqlWorkspace"/> implementation. Loads .sqlproj by
/// parsing the project XML for &lt;Build Include="*.sql"/&gt; entries, reads
/// each script, and feeds them all into a <see cref="TSqlModel"/>.
/// </summary>
public sealed class SqlWorkspace : ISqlWorkspace, IDisposable
{
    private readonly ILogger<SqlWorkspace> _logger;
    private readonly ConcurrentDictionary<string, string> _scripts = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _lastApplied = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Snapshot> _snapshots = new(StringComparer.Ordinal);
    private TSqlModel _model;
    private TSqlModel? _baseline;
    private Dictionary<string, string>? _baselineScripts;

    private SqlWorkspace(string projectPath, TSqlModel model, IDictionary<string, string> scripts, ILogger<SqlWorkspace>? logger)
    {
        ProjectPath = projectPath;
        _model = model;
        _logger = logger ?? NullLogger<SqlWorkspace>.Instance;
        foreach (var kv in scripts)
        {
            _scripts[kv.Key] = kv.Value;
            _lastApplied[kv.Key] = kv.Value;
        }
    }

    /// <inheritdoc/>
    public TSqlModel Model => _model;

    /// <inheritdoc/>
    public string ProjectPath { get; }

    /// <inheritdoc/>
    public IReadOnlyList<string> ScriptPaths => _scripts.Keys.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

    /// <inheritdoc/>
    public TSqlModel? BaselineModel => _baseline;

    /// <inheritdoc/>
    public string? GetScriptText(string path) => _scripts.TryGetValue(path, out var s) ? s : null;

    /// <inheritdoc/>
    public void UpdateScript(string path, string newText)
    {
        _scripts[path] = newText;
        RebuildModel();
    }

    /// <inheritdoc/>
    public string CreateSnapshot(string name, string description)
    {
        var id = Guid.NewGuid().ToString("N");
        _snapshots[id] = new Snapshot(id, name, description, DateTime.UtcNow, new Dictionary<string, string>(_scripts, StringComparer.OrdinalIgnoreCase));
        return id;
    }

    /// <inheritdoc/>
    public IGenericResult<TSqlModel> RestoreSnapshot(string snapshotId)
    {
        if (string.IsNullOrWhiteSpace(snapshotId))
            return GenericResult<TSqlModel>.Failure(SqlWorkspaceResultCodes.SnapshotIdRequired);
        if (!_snapshots.TryGetValue(snapshotId, out var snap))
            return GenericResult<TSqlModel>.Failure(SqlWorkspaceResultCodes.SnapshotNotFound, ResultDetails.Create("SnapshotId", snapshotId));
        _scripts.Clear();
        foreach (var kv in snap.Scripts) _scripts[kv.Key] = kv.Value;
        RebuildModel();
        return GenericResult<TSqlModel>.Success(_model);
    }

    /// <inheritdoc/>
    public void SetBaseline()
    {
        _baseline = _model;
        _baselineScripts = new Dictionary<string, string>(_scripts, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public int RevertToBaseline()
    {
        if (_baselineScripts is null) return 0;
        var changed = 0;
        _scripts.Clear();
        foreach (var kv in _baselineScripts)
        {
            _scripts[kv.Key] = kv.Value;
            changed++;
        }
        RebuildModel();
        return changed;
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<IReadOnlyList<string>>> ApplyChanges(CancellationToken cancellationToken = default)
    {
        var written = new List<string>();
        var failures = new List<IGenericResult>();
        foreach (var path in _scripts.Keys.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = _scripts[path];
            if (_lastApplied.TryGetValue(path, out var prev) && string.Equals(prev, current, StringComparison.Ordinal))
                continue;

            var fileResult = await WriteFile(path, current, cancellationToken).ConfigureAwait(false);
            if (fileResult.IsSuccess)
            {
                _lastApplied[path] = current;
                written.Add(path);
            }
            else
            {
                failures.Add(fileResult);
            }
        }
        if (failures.Count > 0)
        {
            var aggregated = string.Join(" || ",
                failures.Select(f => f.CurrentMessage ?? f.Details?.ToString() ?? "Unknown error"));
            return GenericResult<IReadOnlyList<string>>.Failure(
                SqlWorkspaceResultCodes.ApplyChangesFailed,
                ResultDetails.Create("WrittenCount", written.Count)
                    .With("ErrorCount", failures.Count)
                    .With("Errors", aggregated));
        }
        return GenericResult<IReadOnlyList<string>>.Success(written);
    }

    private static async Task<IGenericResult> WriteFile(string path, string content, CancellationToken cancellationToken)
    {
        try
        {
            await File.WriteAllTextAsync(path, content, cancellationToken).ConfigureAwait(false);
            return GenericResult.Success();
        }
        catch (IOException ex)
        {
            return GenericResult.Failure(
                SqlWorkspaceResultCodes.ApplyChangesFailed,
                ResultDetails.Create("Path", path).With("Exception", ex.GetType().Name).With("ExceptionMessage", ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            return GenericResult.Failure(
                SqlWorkspaceResultCodes.ApplyChangesFailed,
                ResultDetails.Create("Path", path).With("Exception", ex.GetType().Name).With("ExceptionMessage", ex.Message));
        }
    }

    private void RebuildModel()
    {
        var newModel = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        foreach (var kv in _scripts.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            newModel.AddObjects(kv.Value);
        }
        var old = _model;
        _model = newModel;
        old.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _model.Dispose();
        _baseline?.Dispose();
    }

    /// <summary>Loads a .sqlproj into a workspace by parsing the project XML and reading every &lt;Build/&gt; include.</summary>
    public static async Task<IGenericResult<ISqlWorkspace>> Load(string sqlprojPath, ILogger<SqlWorkspace>? logger = null, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sqlprojPath))
            return GenericResult<ISqlWorkspace>.Failure(SqlWorkspaceResultCodes.ProjectNotFound, ResultDetails.Create("Path", sqlprojPath));

        var projectDir = Path.GetDirectoryName(Path.GetFullPath(sqlprojPath))!;
        var doc = await Task.Run(() => XDocument.Load(sqlprojPath), cancellationToken).ConfigureAwait(false);
        var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var scriptPaths = new List<string>();
        foreach (var build in doc.Descendants(ns + "Build"))
        {
            var include = build.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include)) continue;
            var resolved = Path.GetFullPath(Path.Combine(projectDir, include.Replace('\\', Path.DirectorySeparatorChar)));
            if (File.Exists(resolved)) scriptPaths.Add(resolved);
        }
        // Fallback: enumerate every .sql under the project dir if the .sqlproj had no Build entries
        if (scriptPaths.Count == 0)
        {
            scriptPaths.AddRange(Directory.EnumerateFiles(projectDir, "*.sql", SearchOption.AllDirectories));
        }

        var model = new TSqlModel(SqlServerVersion.Sql160, new TSqlModelOptions());
        var scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var parseFailures = new List<IGenericResult>();
        foreach (var path in scriptPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            scripts[path] = text;
            var addResult = AddScriptToModel(model, text, path);
            if (!addResult.IsSuccess)
                parseFailures.Add(addResult);
        }

        var ws = new SqlWorkspace(Path.GetFullPath(sqlprojPath), model, scripts, logger);
        if (parseFailures.Count > 0)
        {
            var summary = string.Join(" || ",
                parseFailures.Select(f => f.CurrentMessage ?? f.Details?.ToString() ?? "Unknown parse error"));
            return GenericResult<ISqlWorkspace>.Success(ws,
                $"Loaded {scripts.Count} script(s); {parseFailures.Count} failed parse: {summary}");
        }
        return GenericResult<ISqlWorkspace>.Success(ws);
    }

    private static IGenericResult AddScriptToModel(TSqlModel model, string text, string path)
    {
        try
        {
            model.AddObjects(text);
            return GenericResult.Success();
        }
#pragma warning disable CA1031 // any parser exception is captured into the workspace result so callers can inspect it
        catch (Exception ex)
#pragma warning restore CA1031
        {
            return GenericResult.Failure(
                SqlWorkspaceResultCodes.ApplyChangesFailed,
                ResultDetails.Create("Path", path).With("Exception", ex.GetType().Name).With("ExceptionMessage", ex.Message));
        }
    }

    private sealed record Snapshot(string Id, string Name, string Description, DateTime CreatedAt, Dictionary<string, string> Scripts);
}
