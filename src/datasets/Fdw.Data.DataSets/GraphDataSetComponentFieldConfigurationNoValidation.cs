using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetComponentFieldConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetComponentFieldConfiguration")]
public sealed class GraphDataSetComponentFieldConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetComponentFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentFieldConfigurationNoValidation"/> class.</summary>
    public GraphDataSetComponentFieldConfigurationNoValidation()
        : base("GraphDataSetComponentFieldConfiguration", "A component's list settings are checked against the component's kind when the graph is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
