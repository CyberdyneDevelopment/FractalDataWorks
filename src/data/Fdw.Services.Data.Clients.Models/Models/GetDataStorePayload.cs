namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// The body of a read of one data store: <c>POST /datastores/get</c>.
/// </summary>
/// <remarks>
/// A read of one record is a POST, not a GET: a GET is a plain paged list and nothing else.
/// </remarks>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class GetDataStorePayload
{
    /// <summary>Gets or sets the data store name.</summary>
    public string Name { get; set; } = string.Empty;
}
