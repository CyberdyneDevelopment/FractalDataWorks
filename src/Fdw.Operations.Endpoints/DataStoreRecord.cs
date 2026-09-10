using System;
using Fdw.Data;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Database record representing a data store, as declared by the data.DataStore container.
/// </summary>
/// <remarks>
/// This type modelled the DataStoreConfiguration table, which no longer exists. The container
/// name was corrected to DataStore, so the query began succeeding and then failed one step
/// later on the mapper -- the SELECT projects the container's declared fields and nothing here
/// matched them. Location and TranslatorType have no counterpart on data.DataStore at all:
/// where a store lives is now the connection's business, reached through ConnectionId.
/// </remarks>
[GenerateMapper]
public partial class DataStoreRecord
{
    /// <summary>Gets or sets the durable logical identity.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the store name, as referenced by DataSetSource.DataStoreName.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the store kind (MsSql, Http, FileSystem).</summary>
    public string? Implementation { get; set; }

    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the connection this store reads and writes through.</summary>
    public Guid? ConnectionId { get; set; }
}
