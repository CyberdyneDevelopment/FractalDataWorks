namespace Fdw.Web.Http.Authentication;

using System.Threading.Tasks;

/// <summary>
/// Notifies the authentication system when a session has expired
/// (refresh token rejected). Implementations clear local tokens
/// and trigger auth state change so UI redirects to login.
/// </summary>
public interface IAuthExpirationNotifier
{
    /// <summary>Notifies that the current session has expired and tokens should be cleared.</summary>
    Task NotifySessionExpired();
}
