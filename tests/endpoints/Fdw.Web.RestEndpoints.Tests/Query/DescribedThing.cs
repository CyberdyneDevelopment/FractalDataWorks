namespace Fdw.Web.RestEndpoints.Tests.Query;

/// <summary>A domain record with one column the store holds (Name) and one member a provider fills after the read (Description).</summary>
public sealed class DescribedThing
{
    /// <summary>Gets or sets the name, a column.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the description, filled by the provider and held by no column.</summary>
    public string? Description { get; set; }
}
