using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.TUI.Management.Services;

/// <summary>
/// Reads, runs and refreshes the generated Newman suite that covers the API surface.
/// </summary>
/// <remarks>
/// The suite is a Postman collection generated from the API's OpenAPI document, plus the
/// scripts that produce and run it. This service is the seam between that folder on disk
/// and the screen that drives it, so the screen never shells out or parses JSON itself.
/// </remarks>
public interface INewmanSuiteService
{
    /// <summary>Gets the folders in the generated collection and how many requests each holds.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The folders, or a failure describing why the collection could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<NewmanFolder>>> GetFolders(CancellationToken cancellationToken = default);

    /// <summary>Runs the suite, or one folder of it.</summary>
    /// <param name="folder">The folder to run, or null for the whole suite.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The run outcome, or a failure describing why it could not run.</returns>
    Task<IGenericResult<NewmanRun>> Run(string? folder, CancellationToken cancellationToken = default);

    /// <summary>Reads the assertions that failed in the last recorded run.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The failures, or a failure describing why they could not be read.</returns>
    Task<IGenericResult<IReadOnlyList<NewmanFailure>>> GetLastFailures(CancellationToken cancellationToken = default);

    /// <summary>Pulls a fresh OpenAPI document and regenerates the collection from it.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the regeneration produced, or a failure describing why it did not.</returns>
    Task<IGenericResult<NewmanRefresh>> Refresh(CancellationToken cancellationToken = default);
}
