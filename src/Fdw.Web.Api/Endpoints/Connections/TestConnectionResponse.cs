namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Response DTO containing the result of a connection test.
/// </summary>
public class TestConnectionResponse
{
    /// <summary>Gets or sets the connection name that was tested.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets whether the connection test succeeded.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the result message describing the test outcome.</summary>
    public required string Message { get; set; }
}
