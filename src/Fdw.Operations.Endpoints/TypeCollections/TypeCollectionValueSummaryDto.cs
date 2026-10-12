using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Summary of a TypeOption within a TypeCollection.
/// </summary>
public sealed class TypeCollectionValueSummaryDto
{
    /// <summary>Gets or sets the option's id; for a collection whose ids are declared integers this is its declaration order.</summary>
    public object? Id { get; set; }

    /// <summary>Gets or sets the TypeOption name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the label to show a person, or null when the option carries none.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the help text for the option, or null when the option carries none.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the option's category, or null when the option carries none.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the optional expected configuration properties for this TypeOption.</summary>
    public IReadOnlyList<string> ExpectedProperties { get; set; } = [];

    /// <summary>Gets or sets the required configuration properties for this TypeOption.</summary>
    public IReadOnlyList<string> RequiredProperties { get; set; } = [];

    /// <summary>Gets or sets the parameters the option takes, for the console to build its picker from; empty when it declares none.</summary>
    public IReadOnlyList<TypeCollectionValueParameterDto> Parameters { get; set; } = [];

    /// <summary>
    /// Gets or sets how many stages of this kind may nest inside one another (FDW-1120); null for an option that holds no
    /// stages of its own. Only a Branch stage states it today.
    /// </summary>
    public int? MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets the names of the pipeline types this option runs on (FDW-1120 phase 4), as a pipeline's pipelineType states
    /// them; null for an option that runs on every pipeline type.
    /// </summary>
    public IReadOnlyList<string>? PipelineTypes { get; set; }

    /// <summary>
    /// Gets or sets whether this option can write the records a pipeline's stages rejected (FDW-1148); null for an option that
    /// does not say. A load step kind states it, and a reject step may use only a kind that does.
    /// </summary>
    public bool? AcceptsRejectedRecords { get; set; }

    /// <summary>
    /// Gets or sets the names of the columns the engine fills in for this option (FDW-1148); null for an option that does not
    /// say, an empty list for one that reserves none. The Reject load phase states the two a reject step's destination needs.
    /// </summary>
    public IReadOnlyList<string>? ReservedColumns { get; set; }
}
