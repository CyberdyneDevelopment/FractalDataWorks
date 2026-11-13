using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetFieldConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetFieldConfiguration")]
public sealed class GraphDataSetFieldConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetFieldConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldConfigurationNoValidation"/> class.</summary>
    public GraphDataSetFieldConfigurationNoValidation()
        : base("GraphDataSetFieldConfiguration", "A field out names the component output that feeds it; whether that output exists is checked against the graph when the graph body is stored (the next slice of FDW-5287). Replace this with a validator when its rules are stated.")
    {
    }
}
