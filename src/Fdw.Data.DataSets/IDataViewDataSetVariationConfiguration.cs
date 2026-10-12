namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The data view kind of a data set variation: a variation plus the chart it draws.</summary>
public interface IDataViewDataSetVariationConfiguration : IDataSetVariationImplementationConfiguration
{
    /// <summary>Gets or sets the JSON of what the chart plots.</summary>
    string Encoding { get; set; }

    /// <summary>Gets or sets the chart type.</summary>
    string ChartType { get; set; }
}
