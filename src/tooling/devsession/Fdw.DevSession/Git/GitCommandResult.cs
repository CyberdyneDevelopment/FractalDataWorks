namespace Fdw.DevSession.Git;

/// <summary>The raw outcome of one git invocation.</summary>
/// <remarks>
/// Carries the exit code alongside both streams rather than collapsing to success/failure, because
/// several callers need a non-zero exit to mean something specific (e.g. `git diff --cached --quiet`
/// exits 1 precisely when there ARE staged changes) and would otherwise lose that distinction.
/// </remarks>
public sealed class GitCommandResult
{
    /// <summary>Initializes the result of a git invocation.</summary>
    public GitCommandResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    /// <summary>Gets the process exit code.</summary>
    public int ExitCode { get; }

    /// <summary>Gets the trimmed contents of stdout.</summary>
    public string StandardOutput { get; }

    /// <summary>Gets the trimmed contents of stderr.</summary>
    public string StandardError { get; }

    /// <summary>Gets a value indicating whether git exited zero.</summary>
    public bool IsSuccess => ExitCode == 0;
}
