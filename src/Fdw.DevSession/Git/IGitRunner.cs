using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.DevSession.Git;

/// <summary>Runs a single git command and returns its raw outcome.</summary>
/// <remarks>
/// Exists as a seam so the engine's git grammar can be tested without a real repository, and so the
/// process-launch concern stays out of the engine itself. A failure result means git could not be
/// RUN at all (missing executable, bad working directory); a git command that ran and exited
/// non-zero is a SUCCESS result carrying that exit code, because only the caller knows whether a
/// given non-zero exit is an error.
/// </remarks>
public interface IGitRunner
{
    /// <summary>Runs git with the supplied arguments in the supplied working directory.</summary>
    Task<IGenericResult<GitCommandResult>> Run(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
