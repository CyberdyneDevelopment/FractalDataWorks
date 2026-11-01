namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A Federated data set: sources of several stores combined by a federation strategy.</summary>
public interface IFederatedDataSetConfiguration : IJoinedDataSetConfiguration
{
    /// <summary>Gets or sets how the data set combines its sources: the name of a federation strategy.</summary>
    string? FederationStrategy { get; set; }
}
