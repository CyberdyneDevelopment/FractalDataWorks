namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Request DTO for testing a connection by name.
/// </summary>
public class TestConnectionRequest
{
    /// <summary>Gets or sets the connection name to test.</summary>
    public string Name { get; set; } = "";
}
