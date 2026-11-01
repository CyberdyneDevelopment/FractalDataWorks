using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A data set whose sources are combined by joins: Compound pushes them into one query, Federated runs them in memory.</summary>
public interface IJoinedDataSetConfiguration : IDataSetImplementationConfiguration
{
    /// <summary>Gets or sets the joins between the data set's sources, in the order they run.</summary>
    IList<DataSetJoinConfiguration> Joins { get; set; }
}
