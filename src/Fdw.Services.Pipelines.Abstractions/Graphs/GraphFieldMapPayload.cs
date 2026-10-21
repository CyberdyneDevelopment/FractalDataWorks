using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One field mapping of a component setting (FDW-1396): a target field from a source field or a typed constant, and for a setting that takes them
/// (an input's <c>mappings</c>, FDW-2000) a type to convert to, a default, a required flag and a chain of Transformations.
/// </summary>
public class GraphFieldMapPayload
{
    /// <summary>Gets or sets the input node the mapping belongs to, on a component that maps each input; none elsewhere.</summary>
    public string? Input { get; set; }

    /// <summary>Gets or sets the output field.</summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>Gets or sets the incoming field the value comes from; none when the mapping is a constant.</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets the constant; none when the mapping takes a source field.</summary>
    public GraphConstantPayload? Constant { get; set; }

    /// <summary>Gets or sets the type the value is converted to (the exact name of a data type, such as <c>Int32</c> or <c>DateTime</c>); none leaves the value as it arrives.</summary>
    public string? TargetType { get; set; }

    /// <summary>Gets or sets the text used when the source field is missing or null, read as <see cref="TargetType"/> when one is stated; none means a missing source is refused.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Gets or sets a value stating that a null result is refused; none is the same as false.</summary>
    public bool? IsRequired { get; set; }

    /// <summary>Gets or sets the Transformations applied to the value, in order, each named by name or by id.</summary>
    public System.Collections.Generic.IList<PipelineFieldTransformationReference>? Transformations { get; set; }
}
