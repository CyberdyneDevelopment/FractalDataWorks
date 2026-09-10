using System.Collections.Generic;
using System;
using Fdw.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The contract every DataSet implementation carries.
/// </summary>
/// <remarks>
/// The marker is what keeps the domain closed: only a configuration carrying it can be
/// registered against this domain or handed back by a read of it.
/// </remarks>
public interface IDataSetImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the data set's Description.</summary>
    string Description { get; set; }

    /// <summary>Gets or sets the data set's Fields.</summary>
    IList<DataSetFieldConfiguration> Fields { get; set; }

    /// <summary>Gets or sets the data set's Joins.</summary>
    IList<JoinConfiguration> Joins { get; set; }

    /// <summary>Gets or sets the data set's Sources.</summary>
    IList<DataSetSourceConfiguration> Sources { get; set; }

    /// <summary>Gets or sets the data set's Aggregates.</summary>
    IList<DataSetAggregateConfiguration> Aggregates { get; set; }

    /// <summary>Gets or sets how a federated data set combines its sources.</summary>
    string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid DataSetId { get; set; }

}
