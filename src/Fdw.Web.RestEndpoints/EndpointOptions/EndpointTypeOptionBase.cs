using System;
using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.EndpointOptions;

/// <summary>
/// Base for a declared endpoint. Carries the endpoint's type and its registration switch.
/// </summary>
/// <remarks>
/// Identity reaches the collection through this constructor rather than through overridden
/// properties, matching how every other option family in the framework is built —
/// DevelopmentCommandBase, RoslynCommandBase and SqlCommandBase all take their values as
/// constructor arguments and hand a derived id to the base.
/// </remarks>
public abstract class EndpointTypeOptionBase : TypeOptionBase<int, EndpointTypeOptionBase>, IEndpointTypeOption
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointTypeOptionBase"/> class.
    /// </summary>
    /// <param name="name">The option's name — its discriminator within the collection.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    /// <param name="category">The option's category; defaults to <c>Endpoint</c>.</param>
    protected EndpointTypeOptionBase(
        string name,
        Type endpointType,
        string description,
        string? category = null)
        : base(GenerateIdFromName(name), name, name, name, description, category ?? "Endpoint")
    {
        EndpointType = endpointType ?? throw new ArgumentNullException(nameof(endpointType));
    }

    /// <inheritdoc />
    public Type EndpointType { get; }

    /// <inheritdoc />
    public bool SkipRegistration { get; set; }

    /// <summary>
    /// Derives an option's name from the endpoint class it declares.
    /// </summary>
    /// <remarks>
    /// The trailing "Endpoint" is trimmed so an option reads as the resource operation —
    /// <c>ListServerSettings</c>, not <c>ListServerSettingsEndpoint</c>.
    ///
    /// Protected rather than private because a collection binds to one non-generic member base, so
    /// every resource declares its own pair: a closed base for the collection and a generic one for
    /// members to close. The generic half needs this to derive its name, and duplicating the trim
    /// per resource is how the convention would drift.
    /// </remarks>
    /// <param name="endpointType">The endpoint class.</param>
    /// <returns>The option name for that endpoint.</returns>
    protected static string DeriveName(Type endpointType)
    {
        if (endpointType is null)
        {
            throw new ArgumentNullException(nameof(endpointType));
        }

        var name = endpointType.Name;
        return name.EndsWith("Endpoint", StringComparison.Ordinal) && name.Length > "Endpoint".Length
            ? name.Substring(0, name.Length - "Endpoint".Length)
            : name;
    }

    /// <summary>
    /// Derives a stable identifier from an option's name.
    /// </summary>
    /// <remarks>
    /// FNV-1a over the name, masked to stay non-negative. The same derivation every other option
    /// base uses, so an endpoint's id is stable across builds and machines — it is a hash of the
    /// name, never a counter or a <c>GetHashCode</c>, which .NET does not guarantee between runs.
    /// </remarks>
    /// <param name="name">The option's name.</param>
    /// <returns>A stable identifier for an option of that name.</returns>
    protected static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;
            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }

            return hash & 0x7FFFFFFF;
        }
    }
}
