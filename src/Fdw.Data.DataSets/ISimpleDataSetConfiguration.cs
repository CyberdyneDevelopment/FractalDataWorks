using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A Simple data set: one source, read in one query, with the aggregates it defines for itself.</summary>
public interface ISimpleDataSetConfiguration : IDataSetImplementationConfiguration
{
    /// <summary>Gets or sets the aggregates the data set defines, so that it reads as a grouped result.</summary>
    IList<DataSetAggregateConfiguration> Aggregates { get; set; }
}
