namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// Limits a sign-in flow to holders of one permission.
/// </summary>
public class SetAuthenticationFlowPermissionRequest : AuthenticationFlowRouteRequest
{
    /// <summary>Gets or sets the permission, e.g. <c>signin:password</c>.</summary>
    public string Permission { get; set; } = string.Empty;
}
