using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Transformations.Abstractions;

namespace Fdw.Services.Pipelines.Abstractions.TypeCollections.PipelineTaskTypeOptions;

/// <summary>
/// Base class for pipeline task type definitions using the CRTP pattern.
/// </summary>
/// <remarks>
/// All concrete task types pass their static field list via the constructor. The default
/// is an empty list — Source/Destination/Transform/Trash all use an empty list because
/// their configurable fields come from the bound connection or transformation type at
/// render time, not from the task type itself.
/// </remarks>
public abstract class PipelineTaskTypeBase : TypeOptionBase<int, PipelineTaskTypeBase>, IPipelineTaskType
{
    private readonly IReadOnlyList<TransformFieldDescriptor> _configurationFields;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineTaskTypeBase"/> class.
    /// </summary>
    /// <param name="id">The unique numeric identifier for this task type.</param>
    /// <param name="name">The unique name used for lookup via <c>PipelineTaskTypes.ByName()</c>.</param>
    /// <param name="configurationFields">
    /// Task-type-level configuration fields. Pass an empty list for Source/Destination/Transform/Trash
    /// where fields come from the connection or transformation binding instead.
    /// </param>
    protected PipelineTaskTypeBase(
        int id,
        string name,
        IReadOnlyList<TransformFieldDescriptor>? configurationFields = null)
        : base(id, name)
    {
        _configurationFields = configurationFields ?? [];
    }

    /// <inheritdoc/>
    public IReadOnlyList<TransformFieldDescriptor> ConfigurationFields => _configurationFields;
}
