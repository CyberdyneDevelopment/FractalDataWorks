namespace Fdw.Services.Data.Clients.Models;

/// <summary>A data set's own value of one federation strategy option (FDW-6901): ReadsInParallel, MaxParallelReads or OrdersSources.</summary>
public sealed class DataSetFederationOptionPayload
{
    /// <summary>Gets or sets the option's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text: true or false, or a whole number of at least one for MaxParallelReads.</summary>
    public string Value { get; set; } = string.Empty;
}
