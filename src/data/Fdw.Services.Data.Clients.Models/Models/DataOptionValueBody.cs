
namespace Fdw.Services.Data.Clients.Models;

/// <summary>One value of a data option rule to add (FDW-6339): a literal or a parameter, never both.</summary>
public sealed class DataOptionValueBody
{
    /// <summary>Gets or sets the value's name; optional.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the value's abbreviation; optional.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the kind of value: Literal or Parameter.</summary>
    public string? Kind { get; set; }

    /// <summary>Gets or sets the literal value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the name of the parameter a run supplies.</summary>
    public string? Parameter { get; set; }

    /// <summary>Gets or sets the value's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the value's place in a list.</summary>
    public int? Ordinal { get; set; }
}
