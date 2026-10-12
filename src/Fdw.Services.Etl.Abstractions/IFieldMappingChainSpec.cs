using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// A field mapping that can state a chain of named Transformations. Implemented beside <see cref="IFieldMapping"/> by the
/// caller's request type, so a stage kind that reads a mapping does not have to know about chains.
/// </summary>
public interface IFieldMappingChainSpec
{
    /// <summary>
    /// Gets the chain the mapping states, in the order stated. Empty when it states none: an absent chain and an empty one
    /// are the same thing.
    /// </summary>
    IReadOnlyList<IFieldTransformationReferenceSpec> TransformationReferences { get; }
}
