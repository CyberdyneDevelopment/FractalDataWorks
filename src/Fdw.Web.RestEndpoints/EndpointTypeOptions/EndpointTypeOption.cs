using System;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// Base for a declared endpoint that takes its identity from the endpoint class itself.
/// </summary>
/// <typeparam name="TEndpoint">The endpoint class this option declares.</typeparam>
/// <remarks>
/// Closing this leaves a member with nothing to state twice:
/// <code>
/// [TypeOption(typeof(ServerSettingEndpoints), "ListServerSettings")]
/// public class ListServerSettingsEndpointOption
///     : EndpointTypeOption&lt;ListServerSettingsEndpoint&gt;;
/// </code>
/// The type comes from <typeparamref name="TEndpoint"/>, the name from its type name, and the id
/// from the name. Nothing is repeated, so nothing can disagree — the failure this replaces is an
/// option whose name says one endpoint and whose <c>typeof</c> says another, which compiles.
///
/// TypeCollectionBase already derives its own id and name this way
/// (<c>GenerateIdFromTypeName</c>/<c>GenerateNameFromTypeName</c>); this is the same idea one level
/// down, on the member.
///
/// The generic parameter also lets the compiler reject an option pointed at something that is not
/// an endpoint. A <c>Type</c> property could not: a wrong <c>typeof(...)</c> would build cleanly
/// and fail when the host started.
///
/// The trailing "Endpoint" is trimmed from the name so options read as the resource operation —
/// <c>ListServerSettings</c>, not <c>ListServerSettingsEndpoint</c> — matching how the
/// <c>[TypeOption]</c> attribute names them.
/// </remarks>
public abstract class EndpointTypeOption<TEndpoint> : EndpointTypeOptionBase
    where TEndpoint : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointTypeOption{TEndpoint}"/> class.
    /// </summary>
    protected EndpointTypeOption()
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), $"The {DeriveName(typeof(TEndpoint))} endpoint.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointTypeOption{TEndpoint}"/> class with an
    /// explicit description.
    /// </summary>
    /// <param name="description">What the endpoint does.</param>
    protected EndpointTypeOption(string description)
        : base(DeriveName(typeof(TEndpoint)), typeof(TEndpoint), description)
    {
    }

}
