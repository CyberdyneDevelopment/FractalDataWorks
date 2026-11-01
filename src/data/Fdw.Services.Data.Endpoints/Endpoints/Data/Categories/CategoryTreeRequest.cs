namespace Fdw.Services.Data.Endpoints;

/// <summary>Request for the category tree: all of it, or the hierarchies of one type.</summary>
public class CategoryTreeRequest
{
    /// <summary>Gets or sets the type to read (DataSet or Custom); left out to read every type.</summary>
    public string? Type { get; set; }
}
