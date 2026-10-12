namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for one field participating in a container key being created.
/// </summary>
public class CreateDataStoreContainerKeyFieldRequest
{
    /// <summary>
    /// Gets or sets the name of the participating field. Resolved to the field's Id within the
    /// same request's <see cref="AddDataStoreContainerRequest.Fields"/> — a key can only name a
    /// field the same create call is defining.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>Gets or sets the ordinal position of this field within a composite key.</summary>
    public int Ordinal { get; set; }
}
