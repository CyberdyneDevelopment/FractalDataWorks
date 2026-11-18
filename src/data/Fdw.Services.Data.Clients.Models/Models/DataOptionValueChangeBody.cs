using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>The body of a change to one value of a data option rule (FDW-6339): a field left out keeps the stored one.</summary>
public sealed class DataOptionValueChangeBody
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

    /// <summary>
    /// Gets or sets the optional fields of the value to clear (<c>name</c>, <c>abbreviation</c>, <c>description</c>), by their payload names (such as <c>description</c>). A field left out of
    /// the body keeps the stored value, so clearing is stated here and never by sending null. Naming a field that cannot be cleared is
    /// refused (FieldNotClearable).
    /// </summary>
    public List<string>? Clear { get; set; }
}
