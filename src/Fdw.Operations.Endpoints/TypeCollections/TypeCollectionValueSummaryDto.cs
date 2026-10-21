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
    /// Gets or sets whether this option can write the records a pipeline's stages rejected (FDW-1148); null for an option that
    /// does not say. A load step kind states it, and a reject step may use only a kind that does.
    /// </summary>
    public bool? AcceptsRejectedRecords { get; set; }

    /// <summary>
    /// Gets or sets the names of the columns the engine fills in for this option (FDW-1148); null for an option that does not
    /// say, an empty list for one that reserves none. The Reject load phase states the two a reject step's destination needs.
    /// </summary>
    public IReadOnlyList<string>? ReservedColumns { get; set; }

    /// <summary>
    /// Gets or sets the input nodes of a component kind (FDW-1395); null for an option that is not a component kind, an empty
    /// list for a kind with no inputs, which is a source.
    /// </summary>
    public IReadOnlyList<TypeCollectionValueNodeDto>? Inputs { get; set; }

    /// <summary>
    /// Gets or sets the output nodes of a component kind, error outputs included (FDW-1395); null for an option that is not a
    /// component kind, an empty list for a kind with no outputs, which is a destination.
    /// </summary>
    public IReadOnlyList<TypeCollectionValueNodeDto>? Outputs { get; set; }

    /// <summary>
    /// Gets or sets the colour a console draws an option in (neutral, info, success, warning or error) when the option states one (FDW-2142); null otherwise.
    /// </summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the rank of an option that has a place in an order; higher is more urgent (FDW-2142); null otherwise.</summary>
    public int? Rank { get; set; }

    /// <summary>
    /// Gets or sets whether an alert about something of this kind stays until a person acknowledges it (FDW-2142); null for an option with no such rule.
    /// </summary>
    public bool? NeedsAcknowledgement { get; set; }

    /// <summary>
    /// Gets or sets whether a component kind is an action or a loop (FDW-2041): it moves no rows and has no nodes; null for an option that
    /// is not a component kind.
    /// </summary>
    public bool? IsAction { get; set; }

    /// <summary>
    /// Gets or sets whether a component kind is a loop, which runs the components that name it as their parent (FDW-2041); null for an
    /// option that is not a component kind.
    /// </summary>
    public bool? HoldsComponents { get; set; }
}
