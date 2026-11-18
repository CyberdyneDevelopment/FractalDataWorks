using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One value of a data option rule (FDW-6339): a literal, or the name of a parameter a run supplies.</summary>
public sealed class DataOptionValuePayload
{
    /// <summary>Gets or sets the value's id, by which it is changed and removed.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the value's name; optional.</summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the value's abbreviation; optional.</summary>
    public string? Abbreviation { get; set; }

    /// <summary>Gets or sets the kind of value: Literal or Parameter (a DataOptionValueKinds name).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the literal value; absent for a parameter.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the name of the parameter a run supplies; absent for a literal.</summary>
    public string? Parameter { get; set; }

    /// <summary>Gets or sets the value's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the value's place in a list.</summary>
    public int Ordinal { get; set; }
}
