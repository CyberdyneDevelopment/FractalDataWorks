using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication;

namespace Fdw.TUI.Management.Services.Api;

/// <summary>
/// Supplies the credential of the currently connected Fdw instance to the shared
/// <see cref="BearerTokenHandler"/>, so every registered API client authenticates against whichever
/// instance the user selected.
/// </summary>
/// <remarks>
/// This is the <see cref="IAccessTokenProvider"/> seam the Fdw HTTP auth plumbing already expects — the
/// TUI supplies an implementation rather than inventing its own auth path.
/// </remarks>
public sealed class InstanceAccessTokenProvider : IAccessTokenProvider
{
    private readonly IConnectionManager _connectionManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstanceAccessTokenProvider"/> class.
    /// </summary>
    public InstanceAccessTokenProvider(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    /// <inheritdoc />
    public Task<string?> GetAccessToken(CancellationToken cancellationToken = default) =>
        Task.FromResult(_connectionManager.GetCurrentConnection()?.ApiKey);
}
