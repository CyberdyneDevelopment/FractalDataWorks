using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Claims.Abstractions;

/// <summary>
/// Base class for FDW claim types. The claim's wire name is <see cref="TypeOptionBase{TKey,T}.Name"/>
/// (e.g. <c>tenantId</c>) and is also how <c>ClaimDefinitions.ByName(name)</c> resolves it. Metadata
/// (<see cref="IsArray"/>, <see cref="Destinations"/>) is set in the constructor so the TypeCollection
/// source generator can read it without instantiation.
/// </summary>
public abstract class ClaimDefinitionBase : TypeOptionBase<ClaimDefinitionBase>, IClaimDefinition
{
    /// <summary>Initializes a new instance of the <see cref="ClaimDefinitionBase"/> class.</summary>
    /// <param name="name">The claim's wire name (must match the TypeOption attribute).</param>
    /// <param name="isArray">Whether the claim serializes as a JSON array.</param>
    /// <param name="isSessionBound">Whether the claim is the platform's statement about a session it created, which no other issuer may make.</param>
    /// <param name="destinations">The token destination name(s) the claim is written to.</param>
    protected ClaimDefinitionBase(string name, bool isArray, bool isSessionBound, params string[] destinations)
        : base(name)
    {
        IsArray = isArray;
        IsSessionBound = isSessionBound;
        Destinations = destinations ?? System.Array.Empty<string>();
    }

    /// <inheritdoc />
    public bool IsArray { get; }

    /// <inheritdoc />
    public bool IsSessionBound { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Destinations { get; }
}
