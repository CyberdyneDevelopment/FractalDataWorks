using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.DevSession.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.DevSession.Git;

/// <summary>Runs git by launching the git executable.</summary>
/// <remarks>
/// The git CLI rather than a managed git library: worktrees are the primary mechanic here, and the
/// CLI is the only implementation that supports the full `git worktree` surface. It is also what
/// every human and script in this workspace already uses, so behaviour cannot diverge between what
/// an agent does and what a developer does by hand.
/// </remarks>
public sealed class GitProcessRunner : IGitRunner
{
    private readonly ILogger<GitProcessRunner> _logger;

    /// <summary>Initializes the runner.</summary>
    public GitProcessRunner(ILogger<GitProcessRunner>? logger = null)
    {
        _logger = logger ?? NullLogger<GitProcessRunner>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<GitCommandResult>> Run(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(workingDirectory))
        {
            return GenericResult<GitCommandResult>.Failure(
                WorktreeEngineLog.RepoPathInvalid(_logger, workingDirectory));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Why: ArgumentList quotes each element itself. Building one command string would make
        // branch names and paths containing spaces a quoting bug waiting to happen.
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        var rendered = string.Join(" ", arguments);
        WorktreeEngineLog.GitInvoking(_logger, rendered, workingDirectory);

        using var process = new Process { StartInfo = startInfo };
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) standardOutput.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) standardError.AppendLine(e.Data); };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            // Why: a missing or unusable git executable is an environment failure, not a git
            // result. It must surface as a failed result rather than a non-zero exit code that a
            // caller might interpret as a meaningful git outcome.
            return GenericResult<GitCommandResult>.Failure(
                WorktreeEngineLog.GitUnavailable(_logger, ex.Message));
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Why: WaitForExitAsync also waits for the redirected output streams to reach EOF, so the
        // builders are complete once it returns — no separate synchronous drain is needed.
        using (cancellationToken.Register(() => KillQuietly(process, _logger)))
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }

        WorktreeEngineLog.GitCompleted(_logger, rendered, process.ExitCode);

        return GenericResult<GitCommandResult>.Success(
            new GitCommandResult(
                process.ExitCode,
                standardOutput.ToString().Trim(),
                standardError.ToString().Trim()));
    }

    private static void KillQuietly(Process process, ILogger logger)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
        catch (InvalidOperationException ex)
        {
            // Why: the process exited between the HasExited check and the Kill call, which is a
            // benign race — the caller's cancellation is already the outcome. Observed at trace
            // level rather than swallowed, so it is visible when diagnosing a hung git.
            WorktreeEngineLog.GitUnavailable(logger, ex.Message);
        }
    }
}
