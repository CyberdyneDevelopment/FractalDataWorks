namespace Fdw.Web.Http.Authentication;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstracts how access tokens are obtained for HTTP requests.
/// </summary>
public interface IAccessTokenProvider
{
    /// <summary>
    /// Gets the current access token, or <c>null</c> if unavailable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The access token string, or <c>null</c>.</returns>
    Task<string?> GetAccessToken(CancellationToken cancellationToken = default);
}
