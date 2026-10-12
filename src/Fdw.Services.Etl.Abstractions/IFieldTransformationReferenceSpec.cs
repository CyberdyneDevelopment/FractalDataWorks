using System;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// What a caller states about one step of a field mapping's transformer chain: which named Transformation it applies, and
/// where in the chain it sits.
/// </summary>
/// <remarks>
/// Why a Transformation is referenced by name OR by id (exactly one): the name is what a person writes, and the id is what a
/// read-back returns, so a console can send back what it received unchanged. Both stated, or neither, is refused when the
/// pipeline is saved.
/// </remarks>
public interface IFieldTransformationReferenceSpec
{
    /// <summary>Gets the name of the referenced Transformation, or <see langword="null"/> when it is referenced by id.</summary>
    string? Transformation { get; }

    /// <summary>Gets the id of the referenced Transformation, or <see langword="null"/> when it is referenced by name.</summary>
    Guid? TransformationId { get; }

    /// <summary>Gets the position in the chain, counted from 1.</summary>
    int Ordinal { get; }
}
