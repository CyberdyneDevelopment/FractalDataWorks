using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A data set of several sources that states how they combine, by its joins or as a union: Compound pushes it into one query, Federated runs it
/// in memory.
/// </summary>
public interface IJoinedDataSetConfiguration : IDataSetImplementationConfiguration
{
    /// <summary>Gets or sets the joins between the data set's sources, in the order they run.</summary>
    IList<DataSetJoinConfiguration> Joins { get; set; }

    /// <summary>
    /// Gets or sets how the data set's sources combine: the name of a <see cref="Fdw.Data.Abstractions.DataSetCompositionTypes"/> member that takes several sources, Join or Union (FDW-6946); null
    /// when the data set states none, which only a data set with one source may do.
    /// </summary>
    string? SourceCombination { get; set; }
}
