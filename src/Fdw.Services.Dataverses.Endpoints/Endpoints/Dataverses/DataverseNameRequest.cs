namespace Fdw.Services.Dataverses.Endpoints;

/// <summary>Addresses a dataverse by name.</summary>
public class DataverseNameRequest
{
    /// <summary>Gets or sets the dataverse name.</summary>
    public string Name { get; set; } = string.Empty;
}
