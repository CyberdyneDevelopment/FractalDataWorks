namespace Fdw.Services.Data.Clients.Models;

/// <summary>One row an add of option rows asks for (FDW-5664); every field is stated.</summary>
public sealed class NewDataOptionRowPayload
{
    /// <summary>Gets or sets the column; one of the container's discovered fields.</summary>
    public string Column { get; set; } = string.Empty;

    /// <summary>Gets or sets the value; not empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the option type.</summary>
    public string OptionType { get; set; } = string.Empty;

    /// <summary>Gets or sets the option.</summary>
    public string Option { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the row is switched on; a row that leaves it out is refused.</summary>
    public bool? Active { get; set; }
}
