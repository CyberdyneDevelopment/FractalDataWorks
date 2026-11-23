namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// A warning the discovery behind a schema graph returned with its success: something the graph states with less than the catalog
/// holds, such as a foreign key whose target was not read or key metadata the login cannot see (FDW-6309).
/// </summary>
public class SchemaGraphWarningDto
{
    /// <summary>Gets or sets the message's code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the message text.</summary>
    public string Message { get; set; } = string.Empty;
}
