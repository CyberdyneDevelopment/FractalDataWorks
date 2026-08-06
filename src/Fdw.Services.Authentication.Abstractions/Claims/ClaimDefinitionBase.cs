using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// Base class for FDW claim types. The claim's wire name is <see cref="TypeOptionBase{TKey,T}.Name"/>
/// (e.g. <c>tenantId</c>) and is also how <c>ClaimDefinitions.ByName(name)</c> resolves it. Metadata
/// (<see cref="IsArray"/>, <see cref="Destinations"/>) is set in the constructor so the TypeCollection
/// source generator can read it without instantiation.
/// </summary>
public abstract class ClaimDefinitionBase : TypeOptionBase<int, ClaimDefinitionBase>, IClaimDefinition
{
    /// <summary>Initializes a new instance of the <see cref="ClaimDefinitionBase"/> class.</summary>
    /// <param name="id">Unique identifier for this claim type.</param>
    /// <param name="name">The claim's wire name (must match the TypeOption attribute).</param>
    /// <param name="isArray">Whether the claim serializes as a JSON array.</param>
    /// <param name="destinations">The token destination name(s) the claim is written to.</param>
    protected ClaimDefinitionBase(int id, string name, bool isArray, params string[] destinations)
        : base(id, name)
    {
        IsArray = isArray;
        Destinations = destinations ?? System.Array.Empty<string>();
    }

    /// <inheritdoc />
    public bool IsArray { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Destinations { get; }
}
