using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Validation.Configuration;

namespace Fdw.Data.DataSets;

/// <summary>States that <see cref="GraphDataSetFieldNoteConfiguration"/> is checked by <see cref="GraphDataSetFieldNoteConfigurationValidator"/> when it is written (FDW-5294).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationValidators), "GraphDataSetFieldNoteConfiguration")]
public sealed class GraphDataSetFieldNoteConfigurationValidation : ValidatedConfiguration<GraphDataSetFieldNoteConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFieldNoteConfigurationValidation"/> class.</summary>
    public GraphDataSetFieldNoteConfigurationValidation()
        : base("GraphDataSetFieldNoteConfiguration", new GraphDataSetFieldNoteConfigurationValidator())
    {
    }
}
