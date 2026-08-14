using Fdw.Collections.Attributes;

namespace Fdw.Hosting.UiHost.Endpoints.CookieSignInEndpointOptions;

/// <summary>Declares the cookie sign-out endpoint.</summary>
[TypeOption(typeof(CookieSignInEndpoints), "CookieLogout")]
public class CookieLogoutOption : CookieSignInEndpointBase<CookieLogoutEndpoint>
{
}
