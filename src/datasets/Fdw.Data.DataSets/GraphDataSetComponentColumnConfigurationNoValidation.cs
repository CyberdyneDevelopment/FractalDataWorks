using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetComponentColumnConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetComponentColumnConfiguration")]
public sealed class GraphDataSetComponentColumnConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetComponentColumnConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentColumnConfigurationNoValidation"/> class.</summary>
    public GraphDataSetComponentColumnConfigurationNoValidation()
        : base("GraphDataSetComponentColumnConfiguration", "A component's list settings are checked against the component's kind when the graph is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
