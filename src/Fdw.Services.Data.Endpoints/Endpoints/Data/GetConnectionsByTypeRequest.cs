namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for retrieving connections by type.
/// </summary>
public class GetConnectionsByTypeRequest
{
    /// <summary>Gets or sets the connection type name to filter by.</summary>
    public string TypeName { get; set; } = string.Empty;
}
