using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Workspace.Roslyn.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Workspace.Roslyn;

/// <summary>
/// File-based implementation of session storage.
/// </summary>
/// <remarks>
/// <para>
/// Stores sessions in the platform-appropriate location:
/// <list type="bullet">
/// <item><description>Linux: ~/.local/share/roslyn-mcp/sessions/</description></item>
/// <item><description>Windows: %LOCALAPPDATA%/roslyn-mcp/sessions/</description></item>
/// <item><description>macOS: ~/Library/Application Support/roslyn-mcp/sessions/</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires Roslyn MSBuildWorkspace
public sealed class FileBasedSessionStore : ISessionStore, IProjectIndexStore
{
    private const string SessionsDirectory = "sessions";
    private const string ClaudeDirectory = ".claude";
    private const string SessionIndexFileName = "roslyn.sessions";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<FileBasedSessionStore> _logger;
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedSessionStore"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FileBasedSessionStore(ILogger<FileBasedSessionStore> logger)
        : this(logger, GetDefaultBasePath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileBasedSessionStore"/> class
    /// with a custom base path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="basePath">The base path for session storage.</param>
    public FileBasedSessionStore(ILogger<FileBasedSessionStore> logger, string basePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
    }

    /// <inheritdoc />
    public string BasePath => _basePath;

    /// <inheritdoc />
    public async Task<PersistedSession?> LoadSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var path = GetSessionPath(sessionId);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<PersistedSession>(json, s_jsonOptions);
        }
        catch (Exception ex)
        {
            RoslynWorkspaceLog.SessionLoadFailed(_logger, ex, sessionId, ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> SaveSession(
        PersistedSession session,
        CancellationToken cancellationToken = default)
    {
        var ensureResult = EnsureStoreExists();
        if (!ensureResult.IsSuccess)
        {
            return ensureResult;
        }

        var path = GetSessionPath(session.Id);

        try
        {
            var json = JsonSerializer.Serialize(session, s_jsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
            RoslynWorkspaceLog.SessionSavedToPath(_logger, session.Id, path);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionSaveFailed"),
                ResultDetails.Create().With("SessionId", session.Id.ToString()).With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> DeleteSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var path = GetSessionPath(sessionId);

        if (!File.Exists(path))
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("PersistedSessionNotFound"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()));
        }

        try
        {
            await Task.Run(() => File.Delete(path), cancellationToken).ConfigureAwait(false);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("SessionDeleteFailed"),
                ResultDetails.Create().With("SessionId", sessionId.ToString()).With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SessionInfo>> ListSessions(
        CancellationToken cancellationToken = default)
    {
        var sessionsDir = Path.Combine(_basePath, SessionsDirectory);
        var sessions = new List<SessionInfo>();

        if (!Directory.Exists(sessionsDir))
        {
            return sessions;
        }

        var files = Directory.GetFiles(sessionsDir, "*.json");

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var persisted = JsonSerializer.Deserialize<PersistedSession>(json, s_jsonOptions);

                if (persisted is not null)
                {
                    sessions.Add(persisted.ToSessionInfo());
                }
            }
            catch (Exception ex)
            {
                RoslynWorkspaceLog.WorkspaceWarning(_logger, ex.Message);
            }
        }

        return sessions;
    }

    /// <inheritdoc />
    public string GetSessionPath(Guid sessionId)
    {
        return Path.Combine(_basePath, SessionsDirectory, $"{sessionId}.json");
    }

    /// <inheritdoc />
    public bool SessionExists(Guid sessionId)
    {
        return File.Exists(GetSessionPath(sessionId));
    }

    /// <inheritdoc />
    public IGenericResult<bool> EnsureStoreExists()
    {
        var sessionsDir = Path.Combine(_basePath, SessionsDirectory);

        try
        {
            Directory.CreateDirectory(sessionsDir);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("StoreDirectoryCreationFailed"),
                ResultDetails.Create().With("Path", sessionsDir).With("ErrorMessage", ex.Message));
        }
    }

    // ========================================================================
    // IProjectIndexStore Implementation
    // ========================================================================

    /// <inheritdoc />
    public async Task<ProjectSessionIndex?> LoadIndex(
        string projectPath,
        CancellationToken cancellationToken = default)
    {
        var path = GetIndexPath(projectPath);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            var index = JsonSerializer.Deserialize<ProjectSessionIndex>(json, s_jsonOptions);

            if (index is not null)
            {
                RoslynWorkspaceLog.ProjectIndexLoaded(_logger, projectPath, index.Sessions.Count);
            }

            return index;
        }
        catch (Exception ex)
        {
            RoslynWorkspaceLog.WorkspaceWarning(_logger, ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> SaveIndex(
        string projectPath,
        ProjectSessionIndex index,
        CancellationToken cancellationToken = default)
    {
        var claudeDir = Path.Combine(projectPath, ClaudeDirectory);

        try
        {
            Directory.CreateDirectory(claudeDir);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("ProjectIndexUpdateFailed"),
                ResultDetails.Create().With("ProjectPath", projectPath).With("ErrorMessage", ex.Message));
        }

        var path = GetIndexPath(projectPath);

        try
        {
            var json = JsonSerializer.Serialize(index, s_jsonOptions);
            await File.WriteAllTextAsync(path, json, cancellationToken).ConfigureAwait(false);
            RoslynWorkspaceLog.ProjectIndexUpdated(_logger, projectPath, index.Sessions.Count);
            return GenericResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return GenericResult<bool>.Failure(
                WorkspaceResultCodes.ByName("ProjectIndexUpdateFailed"),
                ResultDetails.Create().With("ProjectPath", projectPath).With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc />
    public string GetIndexPath(string projectPath)
    {
        return Path.Combine(projectPath, ClaudeDirectory, SessionIndexFileName);
    }

    /// <inheritdoc />
    public bool IndexExists(string projectPath)
    {
        return File.Exists(GetIndexPath(projectPath));
    }

    // ========================================================================
    // Static Helpers
    // ========================================================================

    /// <summary>
    /// Gets the default base path for session storage based on the current platform.
    /// </summary>
    /// <returns>The platform-appropriate base path.</returns>
    public static string GetDefaultBasePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "roslyn-mcp");
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "roslyn-mcp");
        }
        else
        {
            // Linux and other Unix-like systems
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "roslyn-mcp");
        }
    }
}
