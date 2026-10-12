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
            return GenericResult<GitCommandResult>.Failure(
                WorktreeEngineLog.GitUnavailable(_logger, ex.Message));
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

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
            WorktreeEngineLog.GitUnavailable(logger, ex.Message);
        }
    }
}
