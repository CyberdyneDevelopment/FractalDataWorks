using Fdw.Collections;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>Base class for claim sources. Replaces the closed <c>ClaimSource</c> enum.</summary>
public abstract class ClaimSourceBase : TypeOptionBase<int, ClaimSourceBase>, IClaimSource
{
    /// <summary>Initializes a new instance of the <see cref="ClaimSourceBase"/> class.</summary>
    /// <param name="id">The unique identifier for this source.</param>
    /// <param name="name">The name of this source.</param>
    protected ClaimSourceBase(int id, string name)
        : base(id, name)
    {
    }
}
