using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetComponentSplitOutputConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetComponentSplitOutputConfiguration")]
public sealed class GraphDataSetComponentSplitOutputConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetComponentSplitOutputConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentSplitOutputConfigurationNoValidation"/> class.</summary>
    public GraphDataSetComponentSplitOutputConfigurationNoValidation()
        : base("GraphDataSetComponentSplitOutputConfiguration", "A component's list settings are checked against the component's kind when the graph is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
