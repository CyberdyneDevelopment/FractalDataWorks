using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetComponentSettingConfiguration"/> has no validator of its own, and why (FDW-6133).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetComponentSettingConfiguration")]
public sealed class GraphDataSetComponentSettingConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetComponentSettingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentSettingConfigurationNoValidation"/> class.</summary>
    public GraphDataSetComponentSettingConfigurationNoValidation()
        : base("GraphDataSetComponentSettingConfiguration", "A declared setting is judged against its component's type when the graph is saved, which knows the declaration; the row alone cannot be.")
    {
    }
}
