namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// HTTP client response for a connection test.
/// </summary>
/// <remarks>
/// Distinct from <c>Fdw.Services.Connections.Endpoints.TestConnectionResponse</c>,
/// which is the server-side endpoint contract.
/// </remarks>
public sealed class TestConnectionClientResponse
{
    /// <summary>Gets or sets the connection name that was tested.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether the test succeeded.</summary>
    public bool Success { get; set; }
    /// <summary>Gets or sets the result message.</summary>
    public string Message { get; set; } = string.Empty;
}
