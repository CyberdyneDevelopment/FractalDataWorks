namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Switches a sign-in flow on or off.
/// </summary>
public class UpdateAuthenticationFlowRequest : AuthenticationFlowRouteRequest
{
    /// <summary>Gets or sets whether the flow is switched on. Required: it is the only thing this route changes.</summary>
    public bool? IsEnabled { get; set; }
}
