using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for writing data to an external destination.
/// </summary>
/// <remarks>
/// Destination tasks have no task-type-level configuration fields. The properties panel renders
/// the connection-bound fields from <c>IConnectionType.DataQueryType.ConfigurationFields</c>
/// once the user selects a Connection.
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Destination")]
[ExcludeFromCodeCoverage]
public sealed class DestinationTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="DestinationTaskType"/>.</summary>
    public DestinationTaskType()
        : base(id: 2, name: "Destination")
    {
    }
}
