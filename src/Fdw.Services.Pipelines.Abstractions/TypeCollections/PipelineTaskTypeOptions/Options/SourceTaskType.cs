using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions.Options;

/// <summary>
/// Pipeline task type for reading data from an external source.
/// </summary>
/// <remarks>
/// Source tasks have no task-type-level configuration fields. The properties panel renders
/// the connection-bound fields from <c>IConnectionType.DataQueryType.ConfigurationFields</c>
/// once the user selects a Connection (or has a DataSet bound — see Wave 0b).
/// </remarks>
[TypeOption(typeof(PipelineTaskTypes), "Source")]
[ExcludeFromCodeCoverage]
public sealed class SourceTaskType : PipelineTaskTypeBase
{
    /// <summary>Initializes a new instance of <see cref="SourceTaskType"/>.</summary>
    public SourceTaskType()
        : base(id: 1, name: "Source")
    {
    }
}
