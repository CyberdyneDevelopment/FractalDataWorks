using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Request DTO for one key (PrimaryKey, Surrogate, Natural, Foreign) on a container being created.
/// </summary>
public class CreateDataStoreContainerKeyRequest
{
    /// <summary>Gets or sets the key name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the KeyTypes option name (e.g., "PrimaryKey", "Foreign", "Surrogate", "Natural").</summary>
    public string TypeId { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional key description.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the name of the container the referenced key belongs to, for a Foreign key.
    /// Resolved within the same DataStore. Null for a non-referencing key.
    /// </summary>
    public string? ReferencedContainerName { get; set; }

    /// <summary>
    /// Gets or sets the name of the referenced key on <see cref="ReferencedContainerName"/>, for a
    /// Foreign key. Null for a non-referencing key.
    /// </summary>
    public string? ReferencedKeyName { get; set; }

    /// <summary>Gets or sets the fields that participate in this key.</summary>
    public IList<CreateDataStoreContainerKeyFieldRequest> KeyFields { get; set; } = [];
}
