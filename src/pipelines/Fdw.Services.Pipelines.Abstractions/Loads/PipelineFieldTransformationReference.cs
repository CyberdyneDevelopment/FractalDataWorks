namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// One link of the chain a field mapping applies to its value (an entry of a mapping's <c>transforms</c>): the Transformation
/// and its position in the chain.
/// </summary>
/// <remarks>
/// Why a Transformation is named by <see cref="Transformation"/> OR identified by <see cref="TransformationId"/> (exactly one,
/// stating both or neither is refused when a pipeline is written): the name is what a person writes, and the id is what the
/// pipeline keeps. A read-back states both, the id it is stored by and the name the caller can read it under, so a client can
/// show the name and send back what it received; the name is null when the caller cannot read that Transformation.
/// </remarks>
public sealed class PipelineFieldTransformationReference
{
    /// <summary>Gets or sets the name of the Transformation to apply; null when it is identified by <see cref="TransformationId"/>, and in a read-back when the caller cannot read the Transformation.</summary>
    public string? Transformation { get; set; }

    /// <summary>Gets or sets the id of the Transformation to apply; null when it is named by <see cref="Transformation"/>. A read-back always states this.</summary>
    public System.Guid? TransformationId { get; set; }

    /// <summary>Gets or sets the position in the chain, counted from 1.</summary>
    public int Ordinal { get; set; }
}
