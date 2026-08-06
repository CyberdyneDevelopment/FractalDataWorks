namespace Fdw.Web.Http.Authentication.Blazor;

using System.Threading;

/// <summary>
/// Singleton that holds the current circuit's access token in an <see cref="AsyncLocal{T}"/>.
/// The <see cref="TokenCapturingCircuitHandler"/> sets the value before each inbound circuit
/// activity so that <see cref="BlazorServerAccessTokenProvider"/> can read it without touching HttpContext.
/// </summary>
public sealed class CircuitTokenAccessor
{
    private readonly AsyncLocal<string?> _current = new();

    /// <summary>
    /// Gets or sets the access token for the current async flow.
    /// </summary>
    public string? CurrentToken
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
