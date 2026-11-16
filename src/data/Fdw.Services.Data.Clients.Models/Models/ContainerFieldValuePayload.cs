namespace Fdw.Services.Data.Clients.Models;

/// <summary>One distinct value of a container column (FDW-5666).</summary>
public sealed class ContainerFieldValuePayload
{
    /// <summary>Gets or sets the value, as text.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets how many rows of the container carry the value.</summary>
    public long Rows { get; set; }

    /// <summary>Gets or sets whether the value is one the request names as chosen.</summary>
    public bool Chosen { get; set; }
}
