using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// One field mapping of a component setting that is a list of them (FDW-1396): a target field from a source field, or from a
/// typed constant. An input mapping may also convert the value, default it, require it and run it through a chain (FDW-2000).
/// </summary>
/// <remarks>Cascade child of a component, in <c>pipe.PipelineComponentField</c>.</remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "Pipeline")]
public sealed partial class PipelineComponentFieldConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the row's id.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the setting the mapping belongs to (<c>maps</c>, <c>bring</c> or <c>mappings</c>).</summary>
    public string SettingName { get; set; } = string.Empty;

    /// <summary>Gets or sets the input node the mapping belongs to, on a component that maps each input; none elsewhere.</summary>
    public string? InputName { get; set; }

    /// <summary>Gets or sets the position of the mapping within its setting.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the output field.</summary>
    public string TargetField { get; set; } = string.Empty;

    /// <summary>Gets or sets the incoming field the value comes from; none when the mapping is a constant.</summary>
    public string? SourceField { get; set; }

    /// <summary>Gets or sets the type of a constant, a data type name; none when the mapping takes a source field.</summary>
    public string? ConstantType { get; set; }

    /// <summary>Gets or sets a constant's value as text; none when the constant is null.</summary>
    public string? ConstantText { get; set; }

    /// <summary>Gets or sets the type the mapped value is converted to, the name of a data type (<c>Int32</c>, <c>DateTime</c>); none when it is not converted. A component's mappings setting only.</summary>
    public string? TargetType { get; set; }

    /// <summary>Gets or sets what a null value is replaced with, as text read as the target type; none when a null stays null. A component's mappings setting only.</summary>
    public string? DefaultText { get; set; }

    /// <summary>Gets or sets whether the mapped value must have a value: a null with no default refuses the row. A component's mappings setting only.</summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the chain of named Transformations the value goes through, in <see cref="PipelineComponentFieldTransformationConfiguration.Ordinal"/> order,
    /// before the conversion. Cascade child (pipe.PipelineComponentFieldTransformation). A component's mappings setting only.
    /// </summary>
    /// <remarks>
    /// Why this holds ids and not runnable steps: a step names a Transformation by id, and turning that id into a transformation type and its
    /// parameters needs the Transformation domain, which a configuration row cannot reach. The run resolves the chain when it starts
    /// (<see cref="Fdw.Services.Etl.Transforms.FieldTransformChains"/>) and hands the result to the build through the build context.
    /// </remarks>
    public IList<PipelineComponentFieldTransformationConfiguration> TransformationChain { get; set; } = [];
}
