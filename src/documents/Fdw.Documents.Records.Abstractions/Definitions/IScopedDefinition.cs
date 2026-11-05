using System;

namespace Fdw.Documents.Records.Abstractions.Definitions;

/// <summary>
/// A document definition held at the platform, a tenant or an organization (FDW-3398): a document type, an attribute, a record kind, a
/// workflow stage, a requirement rule or a link kind. The most specific definition of a code is the one in force, unless a level above
/// locked it; <see cref="ScopedDefinitions"/> decides.
/// </summary>
/// <remarks>
/// The level is read from the ids, so there is no level field to disagree with them: neither id set is the platform, the tenant alone is
/// that tenant, and the organization set is that organization. A definition can be held at any level: an organization defines its own
/// document types, with or without one above it.
/// </remarks>
public interface IScopedDefinition
{
    /// <summary>Gets the definition's public id: a UUIDv7 the application set, never generated for it.</summary>
    Guid Id { get; }

    /// <summary>Gets the code the definition is known by, the same at every level; compared without regard to case.</summary>
    string Code { get; }

    /// <summary>Gets the tenant that holds the definition, or null for one the platform holds.</summary>
    Guid? TenantId { get; }

    /// <summary>Gets the organization that holds the definition, or null for one the platform or a tenant holds.</summary>
    Guid? OrganizationId { get; }

    /// <summary>Gets whether the definition bars the levels below from defining the same code. An organization cannot lock: it has no level below it.</summary>
    bool Locked { get; }
}
