namespace Fdw.Services.Authentication.Clients;

using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Defines the contract for handling forgot password requests.
/// </summary>
public interface IForgotPasswordProvider
{
    /// <summary>
    /// Requests a password reset for the specified identifier (username or email).
    /// </summary>
    /// <param name="identifier">The username or email address.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="ForgotPasswordResult"/> indicating success, redirect, or failure.</returns>
    Task<ForgotPasswordResult> RequestPasswordReset(string identifier, CancellationToken cancellationToken = default);
}
