using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace Fdw.Hosting.UiHost.Endpoints.CookieSignInEndpointOptions;

/// <summary>
/// The endpoint group a Blazor UI host declares: sign in, and sign out.
/// </summary>
/// <remarks>
/// A skin serves pages rather than an API, so this is usually the only group it contributes — and
/// contributing it is what lets the endpoint collection see a host that declared something.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "CookieSignInEndpoints")]
[TypeCollection(typeof(CookieSignInEndpointBase), typeof(IEndpointTypeOption), typeof(CookieSignInEndpoints))]
public partial class CookieSignInEndpoints : EndpointTypeCollectionBase<CookieSignInEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
