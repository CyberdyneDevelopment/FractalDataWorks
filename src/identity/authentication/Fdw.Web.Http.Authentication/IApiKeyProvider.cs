namespace Fdw.Web.Http.Authentication;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstracts how API keys are obtained for HTTP requests.
/// Implement this interface to provide a static API key from configuration,
/// environment variables, or a secret manager.
/// </summary>
public interface IApiKeyProvider
{
    /// <summary>
    /// Gets the API key, or <c>null</c> if unavailable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The API key string, or <c>null</c>.</returns>
    Task<string?> GetApiKey(CancellationToken cancellationToken = default);
}
