namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// Where a claim came from, and therefore what it may be used for.
/// </summary>
/// <remarks>
/// A claim's source decides whether it is a fact or a suggestion. An authority you administer states
/// facts; one you merely trust to authenticate people states suggestions. Losing that distinction is
/// how a provider asserting <c>role: admin</c> becomes an administrator in your system.
/// </remarks>
public enum ClaimSource
{
    /// <summary>Read from a store this platform owns. Usable as authorization input.</summary>
    Local = 1,

    /// <summary>Asserted by an external authority. Advisory until an explicit mapping promotes it.</summary>
    External = 2,

    /// <summary>Derived by a step from other context. Usable, and only as good as that step.</summary>
    Derived = 3,
}
