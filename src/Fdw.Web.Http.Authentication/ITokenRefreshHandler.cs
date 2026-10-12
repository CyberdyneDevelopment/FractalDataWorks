namespace Fdw.Web.Http.Authentication;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Handles token refresh operations for expired or expiring tokens.
/// </summary>
public interface ITokenRefreshHandler
{
    /// <summary>
    /// Gets a value indicating whether token refresh is supported.
    /// </summary>
    bool CanRefresh { get; }

    /// <summary>
    /// Attempts to refresh the current token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if the token was refreshed successfully; otherwise, <c>false</c>.</returns>
    Task<bool> TryRefresh(CancellationToken cancellationToken = default);
}
