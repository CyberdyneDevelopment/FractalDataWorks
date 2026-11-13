using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetComponentConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetComponentConfiguration")]
public sealed class GraphDataSetComponentConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetComponentConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentConfigurationNoValidation"/> class.</summary>
    public GraphDataSetComponentConfigurationNoValidation()
        : base("GraphDataSetComponentConfiguration", "The components of a graph are checked against the component catalogue and for a refused kind when the graph is saved (the refusals of FDW-5287); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
