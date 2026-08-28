using System.ComponentModel;
using System.Text.Json;
using Fdw.DevSession.Abstractions;
using ModelContextProtocol.Server;

namespace Fdw.DevSession.McpServer;

/// <summary>Exposes the development-session domain as MCP tools.</summary>
/// <remarks>
/// <para>
/// This is the single front door an agent speaks to in order to get itself an isolated place to
/// work: open a session (which materializes a branch or worktree), claim the paths it intends to
/// touch, commit, and close. The tools are deliberately thin — every decision, and every refusal,
/// belongs to the domain services, so an agent driving this surface and a human driving git by
/// hand cannot diverge in behaviour.
/// </para>
/// <para>
/// Failures are returned as JSON with <c>ok:false</c> and the domain's own message rather than
/// thrown. An MCP client shows a tool error as an opaque failure, which would hide exactly the
/// information the caller needs — that its scope overlapped another strand, or that its base ref
/// does not exist.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class DevSessionToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    private readonly IDevSessionManager _sessions;
    private readonly IWorkspaceCoordinator _coordinator;
    private readonly IWorktreeEngine _engine;

    /// <summary>Initializes the tool service.</summary>
    public DevSessionToolService(
        IDevSessionManager sessions,
        IWorkspaceCoordinator coordinator,
        IWorktreeEngine engine)
    {
        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    [McpServerTool(Name = "devsession_open")]
    [Description("Open a development session for a fix key, materializing an isolated copy. Re-opening an existing key returns the session already in flight rather than branching twice.")]
    public async Task<string> OpenSession(
        [Description("Stable key for the FIX this session serves, e.g. a ticket id. Sessions dedupe on this.")] string key,
        [Description("Absolute path to the git repository.")] string repoPath,
        [Description("Ref to branch from. Use the checkout's LOCAL HEAD; unpushed commits are carried in.")] string baseRef,
        [Description("Name of the branch to create.")] string branchName,
        [Description("Isolation level: 'Worktree' (separate working directory) or 'Branch' (in place).")] string isolationLevel = "Worktree",
        [Description("Absolute path for the worktree. Required when isolationLevel is Worktree.")] string? worktreePath = null,
        CancellationToken cancellationToken = default)
    {
        var request = new SessionRequest(
            key,
            new IsolationRequest(repoPath, baseRef, branchName) { WorktreePath = worktreePath },
            isolationLevel);

        var result = await _sessions.Open(request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Describe(result.Value!) : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "devsession_open_nested")]
    [Description("Open a nested session under a parent, for a side issue handled while the parent is held.")]
    public async Task<string> OpenNested(
        [Description("Id of the parent session.")] Guid parentSessionId,
        [Description("Stable key for the nested fix.")] string key,
        [Description("Absolute path to the git repository.")] string repoPath,
        [Description("Ref to branch from.")] string baseRef,
        [Description("Name of the branch to create.")] string branchName,
        [Description("Isolation level: 'Worktree' or 'Branch'.")] string isolationLevel = "Worktree",
        [Description("Absolute path for the worktree. Required when isolationLevel is Worktree.")] string? worktreePath = null,
        CancellationToken cancellationToken = default)
    {
        var request = new SessionRequest(
            key,
            new IsolationRequest(repoPath, baseRef, branchName) { WorktreePath = worktreePath },
            isolationLevel);

        var result = await _sessions.OpenNested(parentSessionId, request, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Describe(result.Value!) : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "devsession_list")]
    [Description("List every known development session and its current state.")]
    public string ListSessions()
        => JsonSerializer.Serialize(
            new { ok = true, sessions = _sessions.List().Select(Summarize).ToArray() },
            JsonOptions);

    [McpServerTool(Name = "devsession_get")]
    [Description("Look up one session by its id.")]
    public string GetSession(
        [Description("Id of the session.")] Guid sessionId)
    {
        var result = _sessions.Get(sessionId);
        return result.IsSuccess ? Describe(result.Value!) : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "devsession_sleep")]
    [Description("Put a session to sleep. A sleeping session is reclaimable but keeps its branch and worktree.")]
    public async Task<string> SleepSession(
        [Description("Id of the session.")] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessions.Sleep(sessionId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Describe(result.Value!) : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "devsession_wake")]
    [Description("Wake a sleeping session. Fails if the session was never asleep.")]
    public async Task<string> WakeSession(
        [Description("Id of the session.")] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessions.Wake(sessionId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Describe(result.Value!) : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "devsession_close")]
    [Description("Close a session. Terminal: the session cannot be transitioned again, and its key becomes reusable.")]
    public async Task<string> CloseSession(
        [Description("Id of the session.")] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessions.Close(sessionId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? Describe(result.Value!) : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "devsession_commit")]
    [Description("Stage everything and commit inside the session's isolated copy. Fails loudly when there is nothing to commit.")]
    public async Task<string> CommitSession(
        [Description("Id of the session.")] Guid sessionId,
        [Description("Commit message.")] string message,
        CancellationToken cancellationToken = default)
    {
        var session = _sessions.Get(sessionId);
        if (session.IsFailure) return Failed(session.CurrentMessage);

        var result = await _engine.Commit(session.Value!.Copy, message, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? JsonSerializer.Serialize(new { ok = true, commit = result.Value }, JsonOptions)
            : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "strand_fence")]
    [Description("Claim a non-overlapping set of paths for a strand inside a session. Refused if the paths overlap a live strand's claim.")]
    public async Task<string> FenceStrand(
        [Description("Id of the session.")] Guid sessionId,
        [Description("Identifier for this strand of work.")] string strandId,
        [Description("Repo-relative paths this strand intends to write.")] string[] paths,
        CancellationToken cancellationToken = default)
    {
        var result = await _coordinator
            .FenceStrand(sessionId, new ScopeRequest(strandId, paths), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? JsonSerializer.Serialize(
                new { ok = true, strandId = result.Value!.StrandId, paths = result.Value!.Paths, grantedAt = result.Value!.GrantedAt },
                JsonOptions)
            : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "strand_list")]
    [Description("List the strands fenced inside a session and their states.")]
    public async Task<string> ListStrands(
        [Description("Id of the session.")] Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _coordinator.ListStrands(sessionId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? JsonSerializer.Serialize(
                new
                {
                    ok = true,
                    strands = result.Value!
                        .Select(s => new { strandId = s.StrandId, state = s.State.Name, paths = s.Claim.Paths })
                        .ToArray(),
                },
                JsonOptions)
            : Failed(result.CurrentMessage);
    }

    [McpServerTool(Name = "strand_reconcile")]
    [Description("Mark a strand reconciled, releasing its claim so those paths can be fenced again.")]
    public async Task<string> ReconcileStrand(
        [Description("Id of the session.")] Guid sessionId,
        [Description("Identifier of the strand.")] string strandId,
        CancellationToken cancellationToken = default)
    {
        var result = await _coordinator.Reconcile(sessionId, strandId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? JsonSerializer.Serialize(
                new { ok = true, strandId = result.Value!.StrandId, state = result.Value!.State.Name },
                JsonOptions)
            : Failed(result.CurrentMessage);
    }

    private static string Describe(IDevSession session)
        => JsonSerializer.Serialize(new { ok = true, session = Summarize(session) }, JsonOptions);

    private static object Summarize(IDevSession session)
        => new
        {
            id = session.Id,
            key = session.Key,
            state = session.State.Name,
            isTerminal = session.State.IsTerminal,
            branch = session.Copy.BranchName,
            baseRef = session.Copy.BaseRef,
            repoPath = session.Copy.RepoPath,
            worktreePath = session.Copy.WorktreePath,
            isolation = session.Copy.IsolationLevelName,
            parentSessionId = session.ParentSessionId,
            openedAt = session.OpenedAt,
            lastActiveAt = session.LastActiveAt,
        };

    private static string Failed(string? message)
        => JsonSerializer.Serialize(new { ok = false, error = message }, JsonOptions);
}
