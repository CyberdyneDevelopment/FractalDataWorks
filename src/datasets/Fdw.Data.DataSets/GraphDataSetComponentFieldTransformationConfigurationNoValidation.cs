using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>States that <see cref="GraphDataSetComponentFieldTransformationConfiguration"/> has no validator yet, and why (FDW-5287).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetComponentFieldTransformationConfiguration")]
public sealed class GraphDataSetComponentFieldTransformationConfigurationNoValidation : UnvalidatedConfiguration<GraphDataSetComponentFieldTransformationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetComponentFieldTransformationConfigurationNoValidation"/> class.</summary>
    public GraphDataSetComponentFieldTransformationConfigurationNoValidation()
        : base("GraphDataSetComponentFieldTransformationConfiguration", "A component's list settings are checked against the component's kind when the graph is saved (story FDW-5290); until that check states its rules here this says so. Replace this with a validator when its rules are stated.")
    {
    }
}
