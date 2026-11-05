using System;

namespace Fdw.Documents.Records.Abstractions;

/// <summary>
/// A document a record node is expected to have (FDW-3395), and the document that satisfies it once there is one.
/// </summary>
/// <remarks>Which documents a kind of node requires is the organization's requirement rules (document configuration).</remarks>
public interface IRecordRequirement
{
    /// <summary>Gets the requirement's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the public id of the record node that has the requirement.</summary>
    Guid RecordId { get; }

    /// <summary>Gets the document type expected.</summary>
    string ExpectedDocumentType { get; }

    /// <summary>Gets the public id of the document that satisfies the requirement, or null while none does.</summary>
    Guid? SatisfiedBy { get; }
}
