using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// The value of one attribute of a document or a record node (FDW-3395), unique on its owner and attribute code.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Text"/>, <see cref="Number"/>, <see cref="Date"/> and <see cref="Flag"/> is set: the one the attribute
/// definition's data type names. The others are null.
/// </remarks>
public interface IAttributeValue
{
    /// <summary>Gets the value's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the public id of the document or record node the value belongs to.</summary>
    Guid OwnerId { get; }

    /// <summary>Gets the attribute's code, as the attribute definition names it.</summary>
    string AttributeCode { get; }

    /// <summary>Gets the value of a text attribute, or null.</summary>
    string? Text { get; }

    /// <summary>Gets the value of a number attribute, or null.</summary>
    decimal? Number { get; }

    /// <summary>Gets the value of a date attribute, or null.</summary>
    DateTimeOffset? Date { get; }

    /// <summary>Gets the value of a yes-or-no attribute, or null.</summary>
    bool? Flag { get; }
}
