namespace Fdw.Services.Connections.Clients.Models;

/// <summary>
/// A warning returned with generated DDL: something in the schema the DDL does not carry, such as a foreign key it leaves out
/// (FDW-6309).
/// </summary>
public sealed class GenerateDdlWarning
{
    /// <summary>Gets or sets the message's code.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the message text.</summary>
    public string Message { get; set; } = string.Empty;
}
