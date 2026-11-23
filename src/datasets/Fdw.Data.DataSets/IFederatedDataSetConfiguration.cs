using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A Federated data set: sources of several stores combined by a federation strategy.</summary>
public interface IFederatedDataSetConfiguration : IJoinedDataSetConfiguration
{
    /// <summary>Gets or sets how the data set combines its sources: the name of a federation strategy.</summary>
    string? FederationStrategy { get; set; }

    /// <summary>Gets or sets the data set's own values of its strategy's options, which win over the FDW-level settings (FDW-6901).</summary>
    IList<DataSetFederationOptionConfiguration> FederationOptions { get; set; }
}
