using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Drives the generated Newman suite on disk.
/// </summary>
/// <remarks>
/// The suite ships beside the API it covers, not beside this tool, because it is generated
/// from that API's OpenAPI document and versions with it. This service therefore locates it
/// rather than owning it: FDW_NEWMAN_DIR names it outright, and the fallback is the
/// conventional path next to a sibling reference-api checkout.
///
/// Nothing here invents a value. A missing directory, a missing collection and a missing
/// credential are three different failures and each says which it is, because "it did not
/// work" costs an operator the same hour every time.
/// </remarks>
public sealed class NewmanSuiteService : INewmanSuiteService
{
    private readonly ILogger<NewmanSuiteService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewmanSuiteService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public NewmanSuiteService(ILogger<NewmanSuiteService>? logger)
    {
        this.logger = logger ?? NullLogger<NewmanSuiteService>.Instance;
    }

    private static string SuiteDirectory
    {
        get
        {
            var declared = Environment.GetEnvironmentVariable("FDW_NEWMAN_DIR");
            if (!string.IsNullOrWhiteSpace(declared))
            {
                return declared;
            }

            // The conventional location: a reference-api checkout beside the workspace root.
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "projects", "cyberdynedevelopment", "reference-api", "public", "newman");
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<NewmanFolder>>> GetFolders(CancellationToken cancellationToken = default)
    {
        var dir = SuiteDirectory;
        NewmanSuiteLog.SuiteDirectoryResolved(logger, dir);

        if (!Directory.Exists(dir))
        {
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFolder>>.Failure(
                NewmanSuiteLog.SuiteDirectoryMissing(logger, dir)));
        }

        var collection = Path.Combine(dir, "collection.json");
        if (!File.Exists(collection))
        {
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFolder>>.Failure(
                NewmanSuiteLog.CollectionMissing(logger, collection)));
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(collection));
            var folders = new List<NewmanFolder>();
            if (doc.RootElement.TryGetProperty("item", out var items))
            {
                foreach (var folder in items.EnumerateArray())
                {
                    var name = folder.TryGetProperty("name", out var n) ? n.GetString() : null;
                    var count = folder.TryGetProperty("item", out var inner) ? inner.GetArrayLength() : 0;
                    if (!string.IsNullOrEmpty(name))
                    {
                        folders.Add(new NewmanFolder(name, count));
                    }
                }
            }

            var total = 0;
            foreach (var f in folders)
            {
                total += f.RequestCount;
            }

            NewmanSuiteLog.CollectionRead(logger, folders.Count, total);
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFolder>>.Success(folders));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFolder>>.Failure(
                NewmanSuiteLog.CollectionUnreadable(logger, collection, ex.Message)));
        }
        catch (IOException ex)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFolder>>.Failure(
                NewmanSuiteLog.CollectionUnreadable(logger, collection, ex.Message)));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<NewmanRun>> Run(string? folder, CancellationToken cancellationToken = default)
    {
        var dir = SuiteDirectory;
        if (!Directory.Exists(dir))
        {
            return GenericResult<NewmanRun>.Failure(NewmanSuiteLog.SuiteDirectoryMissing(logger, dir));
        }

        // Why checked here rather than left to the script: the script exits 2 with a message on
        // stderr, and surfacing "exit code 2" to an operator explains nothing.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FDW_TEST_PASSWORD")))
        {
            return GenericResult<NewmanRun>.Failure(NewmanSuiteLog.TestPasswordMissing(logger));
        }

        NewmanSuiteLog.RunStarting(logger, folder ?? "whole suite");

        var args = folder is null ? string.Empty : $" --folder {Quote(folder)}";
        var started = Stopwatch.GetTimestamp();
        var exit = await Shell(dir, $"./run.sh{args}", cancellationToken).ConfigureAwait(false);
        var elapsed = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        // Why the record rather than the exit code: newman exits non-zero when assertions fail,
        // which is a result and not an error. The run record distinguishes the two.
        var record = Path.Combine(dir, "last-run.json");
        if (!File.Exists(record))
        {
            return GenericResult<NewmanRun>.Failure(
                NewmanSuiteLog.RunFailed(logger, $"the runner exited {exit} and wrote no run record"));
        }

        try
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(record, cancellationToken).ConfigureAwait(false));
            var run = doc.RootElement.GetProperty("run");
            var stats = run.GetProperty("stats");
            var requests = stats.GetProperty("requests").GetProperty("total").GetInt32();
            var assertions = stats.GetProperty("assertions").GetProperty("total").GetInt32();
            var failed = stats.GetProperty("assertions").GetProperty("failed").GetInt32();

            NewmanSuiteLog.RunFinished(logger, requests, assertions, failed, elapsed);
            return GenericResult<NewmanRun>.Success(new NewmanRun(requests, assertions, failed, elapsed, folder));
        }
        catch (JsonException ex)
        {
            return GenericResult<NewmanRun>.Failure(NewmanSuiteLog.RunFailed(logger, ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return GenericResult<NewmanRun>.Failure(NewmanSuiteLog.RunFailed(logger, ex.Message));
        }
    }

    /// <inheritdoc />
    public Task<IGenericResult<IReadOnlyList<NewmanFailure>>> GetLastFailures(CancellationToken cancellationToken = default)
    {
        var record = Path.Combine(SuiteDirectory, "last-run.json");
        if (!File.Exists(record))
        {
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFailure>>.Failure(
                NewmanSuiteLog.NoRunRecord(logger, record)));
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(record));
            var failures = new List<NewmanFailure>();
            if (doc.RootElement.GetProperty("run").TryGetProperty("failures", out var list))
            {
                foreach (var f in list.EnumerateArray())
                {
                    var source = f.TryGetProperty("source", out var s) && s.TryGetProperty("name", out var sn)
                        ? sn.GetString() ?? "(unnamed request)"
                        : "(unnamed request)";
                    var error = f.GetProperty("error");
                    var assertion = error.TryGetProperty("test", out var t) ? t.GetString() ?? string.Empty : string.Empty;
                    var detail = error.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
                    failures.Add(new NewmanFailure(source, assertion, detail));
                }
            }

            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFailure>>.Success(failures));
        }
        catch (JsonException ex)
        {
            return Task.FromResult(GenericResult<IReadOnlyList<NewmanFailure>>.Failure(
                NewmanSuiteLog.CollectionUnreadable(logger, record, ex.Message)));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<NewmanRefresh>> Refresh(CancellationToken cancellationToken = default)
    {
        var dir = SuiteDirectory;
        if (!Directory.Exists(dir))
        {
            return GenericResult<NewmanRefresh>.Failure(NewmanSuiteLog.SuiteDirectoryMissing(logger, dir));
        }

        NewmanSuiteLog.RefreshStarting(logger);

        var pull = await Shell(dir, "./refresh-spec.sh", cancellationToken).ConfigureAwait(false);
        if (pull != 0)
        {
            return GenericResult<NewmanRefresh>.Failure(
                NewmanSuiteLog.RefreshFailed(logger, $"pulling the OpenAPI document exited {pull.ToString(CultureInfo.InvariantCulture)}"));
        }

        var generate = await Shell(dir, "python3 generate-collection.py", cancellationToken).ConfigureAwait(false);
        if (generate != 0)
        {
            return GenericResult<NewmanRefresh>.Failure(
                NewmanSuiteLog.RefreshFailed(logger, $"generating the collection exited {generate.ToString(CultureInfo.InvariantCulture)}"));
        }

        var paths = 0;
        var operations = 0;
        var spec = Path.Combine(dir, "spec.json");
        if (File.Exists(spec))
        {
            using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(spec, cancellationToken).ConfigureAwait(false));
            if (doc.RootElement.TryGetProperty("paths", out var p))
            {
                foreach (var path in p.EnumerateObject())
                {
                    paths++;
                    foreach (var verb in path.Value.EnumerateObject())
                    {
                        if (IsMethod(verb.Name))
                        {
                            operations++;
                        }
                    }
                }
            }
        }

        var requests = 0;
        var folders = await GetFolders(cancellationToken).ConfigureAwait(false);
        if (folders.IsSuccess && folders.Value is not null)
        {
            foreach (var f in folders.Value)
            {
                requests += f.RequestCount;
            }
        }

        NewmanSuiteLog.RefreshFinished(logger, paths, operations, requests);
        return GenericResult<NewmanRefresh>.Success(new NewmanRefresh(paths, operations, requests));
    }

    private static bool IsMethod(string name) =>
        string.Equals(name, "get", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "post", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "put", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "patch", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, "delete", StringComparison.OrdinalIgnoreCase);

    private static string Quote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

    /// <summary>Runs a command in the suite directory, streaming its output to this terminal.</summary>
    /// <remarks>
    /// Output is inherited rather than captured: the operator asked to run a test suite and
    /// watching it run is the point. The structured outcome comes from the run record.
    /// </remarks>
    private static async Task<int> Shell(string workingDirectory, string command, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                ArgumentList = { "-lc", command },
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
            },
        };

        process.Start();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return process.ExitCode;
    }
}
