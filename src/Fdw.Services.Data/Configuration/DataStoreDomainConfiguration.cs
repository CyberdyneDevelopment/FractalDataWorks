using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Configuration;

/// <summary>
/// The data stores domain's record: the four properties every domain has, and everything a list of
/// data stores shows.
/// </summary>
/// <remarks>
/// Maps to <c>data.DataStore</c> in ConfigurationDb. Everything a data store is beyond these fields
/// lives on its implementation; these live here, on the domain row, so a page of data stores is one
/// read of one table. Reading a data store stamps them onto its implementation; saving one whose values
/// differ writes a new version of this row from them (FDW-978, FDW-1003). The connection's NAME is not
/// here: it is another domain's, so a list composes it through the connection domain's provider.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
public partial class DataStoreDomainConfiguration : DomainConfiguration
{
    /// <summary>Gets or sets the connection this data store is reached through.</summary>
    public Guid ConnectionId { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets when this data store's schema was last discovered.</summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }

    /// <summary>
    /// Gets or sets how sensitive this data is: the <c>ClassificationId</c> of a <c>DataClassifications</c>
    /// level. <c>NotYetClassified</c> until someone picks one; never empty.
    /// </summary>
    public int ClassificationId { get; set; } = DataClassifications.NotYetClassified.Id;

    /// <summary>Gets or sets when the data store was created: carried across versions of the row.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
