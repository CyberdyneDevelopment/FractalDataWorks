using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>States that <see cref="GraphDataSetIntentConfiguration"/> is checked by <see cref="GraphDataSetIntentConfigurationValidator"/> when it is written (FDW-5294).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetIntentConfiguration")]
public sealed class GraphDataSetIntentConfigurationValidation : ValidatedConfiguration<GraphDataSetIntentConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetIntentConfigurationValidation"/> class.</summary>
    public GraphDataSetIntentConfigurationValidation()
        : base("GraphDataSetIntentConfiguration", new GraphDataSetIntentConfigurationValidator())
    {
    }
}
