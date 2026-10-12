using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// Where a claim came from, and therefore what it may be used for.
/// </summary>
/// <remarks>
/// A claim's source decides whether it is a fact or a suggestion. An authority you administer states
/// facts; one you merely trust to authenticate people states suggestions. Losing that distinction is
/// how a provider asserting <c>role: admin</c> becomes an administrator in your system.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ClaimSourceBase), typeof(IClaimSource), typeof(ClaimSources))]
public abstract partial class ClaimSources : TypeCollectionBase<ClaimSourceBase, IClaimSource>
{
}
