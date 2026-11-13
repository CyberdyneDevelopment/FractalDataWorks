using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetLinkConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetLinkConfiguration")]
public sealed class GraphDataSetLinkConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetLinkConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetLinkConfigurationNoValidation"/> class.</summary>
    public GraphDataSetLinkConfigurationNoValidation()
        : base("GraphDataSetLinkConfiguration", "Whether a link's components and nodes exist is checked against the graph when it is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
