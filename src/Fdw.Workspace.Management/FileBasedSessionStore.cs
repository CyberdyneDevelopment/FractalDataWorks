using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Management.Logging;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Workspace.Management;

/// <summary>
/// File-based implementation of <see cref="IWorkspaceSessionStore"/> that persists
/// sessions as JSON files in a specified directory.
/// </summary>
public sealed class FileBasedSessionStore : IWorkspaceSessionStore
{
    private readonly string _sessionDirectory;
    private readonly ILogger<FileBasedSessionStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedSessionStore"/> class.
    /// </summary>
    /// <param name="sessionDirectory">
    /// The directory to store session files. If not specified, uses
    /// %LOCALAPPDATA%/Fdw/Sessions or ~/.local/share/Fdw/Sessions.
    /// </param>
    /// <param name="logger">Optional logger.</param>
    public FileBasedSessionStore(string? sessionDirectory = null, ILogger<FileBasedSessionStore>? logger = null)
    {
        _sessionDirectory = sessionDirectory ?? GetDefaultSessionDirectory();
        _logger = logger ?? NullLogger<FileBasedSessionStore>.Instance;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        EnsureDirectoryExists();
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<bool>> Save(WorkspaceSession session, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetSessionFilePath(session.Id);
            var json = JsonSerializer.Serialize(session, _jsonOptions);

#if NETSTANDARD2_0
            File.WriteAllText(filePath, json);
            await Task.CompletedTask;
#else
            await File.WriteAllTextAsync(filePath, json, cancellationToken).ConfigureAwait(false);
#endif

            WorkspaceManagementLog.SessionSavedToFile(_logger, session.Id, filePath);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            WorkspaceManagementLog.SessionSaveFailed(_logger, ex, session.Id);
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionSaveFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult<WorkspaceSession>> Load(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetSessionFilePath(sessionId);

            if (!File.Exists(filePath))
                return GenericResult<WorkspaceSession>.Failure(
                    WorkspaceResultCodes.ByName("SessionNotFound"),
                    ResultDetails.Create("SessionId", sessionId));

#if NETSTANDARD2_0
            var json = File.ReadAllText(filePath);
            await Task.CompletedTask;
#else
            var json = await File.ReadAllTextAsync(filePath, cancellationToken).ConfigureAwait(false);
#endif

            var session = JsonSerializer.Deserialize<WorkspaceSession>(json, _jsonOptions);
            if (session is null)
                return GenericResult<WorkspaceSession>.Failure(WorkspaceResultCodes.ByName("SessionDeserializationFailed"));

            WorkspaceManagementLog.SessionLoadedFromFile(_logger, sessionId, filePath);
            return GenericResult<WorkspaceSession>.Success(session);
        }
        catch (Exception ex)
        {
            WorkspaceManagementLog.SessionLoadFailed(_logger, ex, sessionId);
            return GenericResult<WorkspaceSession>.Failure(
                WorkspaceResultCodes.ByName("SessionLoadFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public Task<IGenericResult<bool>> Delete(Guid sessionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetSessionFilePath(sessionId);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                WorkspaceManagementLog.SessionDeleted(_logger, sessionId);
            }

            return Task.FromResult(GenericResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            WorkspaceManagementLog.SessionDeleteFailed(_logger, ex, sessionId);
            return Task.FromResult(GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionDeleteFailed"),
                ResultDetails.Create("ErrorMessage", ex.Message)));
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<SessionInfo>> List(CancellationToken cancellationToken = default)
    {
        var sessions = new List<SessionInfo>();

        if (!Directory.Exists(_sessionDirectory))
            return sessions;

        foreach (var filePath in Directory.GetFiles(_sessionDirectory, "*.session.json"))
        {
            try
            {
                var loadResult = await Load(
                    Guid.Parse(Path.GetFileNameWithoutExtension(filePath).Replace(".session", "")),
                    cancellationToken).ConfigureAwait(false);

                if (loadResult.IsSuccess && loadResult.Value is not null)
                {
                    var session = loadResult.Value;
                    sessions.Add(new SessionInfo
                    {
                        Id = session.Id,
                        OriginalWorkspaceId = session.WorkspaceId,
                        SolutionPath = session.SolutionPath,
                        Name = session.Name,
                        SavedAt = session.SavedAt,
                        SnapshotCount = session.Snapshots.Count,
                        HasBaseline = session.BaselineSnapshot is not null
                    });
                }
            }
            catch (Exception ex)
            {
                WorkspaceManagementLog.SessionLoadFromPathFailed(_logger, ex, filePath);
            }
        }

        return sessions.OrderByDescending(s => s.SavedAt);
    }

    /// <inheritdoc/>
    public Task<bool> Exists(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var filePath = GetSessionFilePath(sessionId);
        return Task.FromResult(File.Exists(filePath));
    }

    private string GetSessionFilePath(Guid sessionId) =>
        Path.Combine(_sessionDirectory, $"{sessionId}.session.json");

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_sessionDirectory))
        {
            Directory.CreateDirectory(_sessionDirectory);
            WorkspaceManagementLog.SessionDirectoryCreated(_logger, _sessionDirectory);
        }
    }

    private static string GetDefaultSessionDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(localAppData))
        {
            // Fallback for Linux/macOS
            var home = Environment.GetEnvironmentVariable("HOME") ?? ".";
            return Path.Combine(home, ".local", "share", "Fdw", "Sessions");
        }

        return Path.Combine(localAppData, "Fdw", "Sessions");
    }
}
