namespace Fdw.Services.Data.Endpoints;

/// <summary>Addresses one transformation by name.</summary>
public class TransformationNameRequest
{
    /// <summary>Gets or sets the transformation's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;
}
