using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetConfiguration")]
public sealed class GraphDataSetConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetConfigurationNoValidation"/> class.</summary>
    public GraphDataSetConfigurationNoValidation()
        : base("GraphDataSetConfiguration", "The header and fields out of a Graph data set are stored by this first slice of FDW-5287; the graph body and the refusal of destination, action and loop components are validated when the body is stored (the next slice). Replace this with a validator when its rules are stated.")
    {
    }
}
