namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The data view kind of a data set variation: a variation plus the chart it draws.</summary>
public interface IDataViewDataSetVariationConfiguration : IDataSetVariationImplementationConfiguration
{
    /// <summary>Gets or sets the JSON of what the chart plots; null for a view that draws no chart.</summary>
    string? Encoding { get; set; }

    /// <summary>Gets or sets the chart type; null for a view that draws no chart.</summary>
    string? ChartType { get; set; }
}
