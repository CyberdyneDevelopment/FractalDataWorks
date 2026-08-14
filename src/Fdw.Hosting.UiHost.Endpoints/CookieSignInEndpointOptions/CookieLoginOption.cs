using Fdw.Collections.Attributes;

namespace Fdw.Hosting.UiHost.Endpoints.CookieSignInEndpointOptions;

/// <summary>Declares the cookie sign-in endpoint.</summary>
[TypeOption(typeof(CookieSignInEndpoints), "CookieLogin")]
public class CookieLoginOption : CookieSignInEndpointBase<CookieLoginEndpoint>
{
}
