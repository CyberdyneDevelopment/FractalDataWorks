namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Names the sign-in flow a route acts on.
/// </summary>
public class AuthenticationFlowRouteRequest
{
    /// <summary>Gets or sets the flow's name, bound from the route.</summary>
    public string Name { get; set; } = string.Empty;
}
