using System;
using System.Collections.Generic;


namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// The endpoint types that registered themselves, for FastEndpoints to route.
/// </summary>
/// <remarks>
/// FastEndpoints offers no per-endpoint registration call — with auto-discovery off, the only way
/// in is <c>EndpointDiscoveryOptions.SourceGeneratorDiscoveredTypes</c>, a list read when
/// <c>AddFastEndpoints</c> runs. So an endpoint cannot hand itself directly to FastEndpoints; it
/// adds itself here, and the host passes the collection across.
///
/// ORDERING: whatever calls <c>AddFastEndpoints</c> must run AFTER every domain's Register, or it
/// reads an empty list and nothing is routed. That is a real constraint, not a detail — a host that
/// gets it wrong starts cleanly and serves 404 for every endpoint. <see cref="Count"/> exists so
/// the caller can assert it is non-zero and fail loudly instead.
/// </remarks>
public static class DeclaredEndpoints
{
    private static readonly List<Type> Declared = new();
    private static readonly System.Threading.Lock Gate = new();


    /// <summary>
    /// Gets the endpoint types declared so far.
    /// </summary>
    public static IReadOnlyList<Type> Types
    {
        get
        {
            lock (Gate)
            {
                return Declared.ToArray();
            }
        }
    }

    /// <summary>
    /// Gets how many endpoint types have registered themselves.
    /// </summary>
    public static int Count
    {
        get
        {
            lock (Gate)
            {
                return Declared.Count;
            }
        }
    }

    /// <summary>
    /// Whether an endpoint type was declared and left switched on.
    /// </summary>
    /// <param name="endpointType">The type discovery offered up.</param>
    /// <returns><c>true</c> to route it, <c>false</c> to leave it out.</returns>
    /// <remarks>
    /// Shaped to be handed straight to <c>EndpointDiscoveryOptions.Filter</c>, which is how an
    /// endpoint that was never declared — or was declared and then skipped — stays unrouted while
    /// the rest of discovery works normally. Turning discovery off entirely instead would mean
    /// naming every assembly to scan, and finding none of them if you got it wrong.
    /// </remarks>
    public static bool IsDeclared(Type endpointType)
    {
        if (endpointType is null)
        {
            return false;
        }

        lock (Gate)
        {
            return Declared.Contains(endpointType);
        }
    }

    /// <summary>
    /// Records an endpoint type as declared.
    /// </summary>
    /// <param name="endpointType">The endpoint class.</param>
    /// <remarks>
    /// Idempotent: an endpoint declared twice is routed once. Registering the same type twice makes
    /// FastEndpoints throw on a duplicate route, so the guard belongs here rather than leaving every
    /// caller to remember it.
    /// </remarks>
    public static void Declare(Type endpointType)
    {
        if (endpointType is null)
        {
            throw new ArgumentNullException(nameof(endpointType));
        }

        lock (Gate)
        {
            if (!Declared.Contains(endpointType))
            {
                Declared.Add(endpointType);
            }
        }
    }

}