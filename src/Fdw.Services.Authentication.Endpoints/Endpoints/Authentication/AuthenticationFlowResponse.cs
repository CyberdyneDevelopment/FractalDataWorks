using System.Collections.Generic;
using Fdw.Services.Authentication.Abstractions.Steps;

namespace Fdw.Services.Authentication.Endpoints;

/// <summary>
/// A configured sign-in flow as an administration console shows it.
/// </summary>
public class AuthenticationFlowResponse
{
    /// <summary>Gets or sets the flow's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the flow's description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether an operator has left the flow switched on.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether this host is serving the flow right now.</summary>
    public bool IsServing { get; set; }

    /// <summary>Gets or sets why a switched-on flow is not being served.</summary>
    public string? Problem { get; set; }

    /// <summary>Gets or sets the RFC 8176 methods the flow proves, e.g. <c>pwd</c>.</summary>
    public IReadOnlyList<string> AuthenticationMethods { get; set; } = [];

    /// <summary>Gets or sets the permissions a principal must hold; empty when every active user is admitted.</summary>
    public IReadOnlyList<string> RequiredPermissions { get; set; } = [];

    /// <summary>Gets or sets whether a single required permission can be set or removed from a console.</summary>
    public bool CanChangeRequiredPermission { get; set; }

    /// <summary>Gets or sets why <see cref="CanChangeRequiredPermission"/> is false, as a sentence to show the person.</summary>
    public string? CanChangeRequiredPermissionReason { get; set; }

    /// <summary>Maps the service's view of a flow onto the wire.</summary>
    /// <param name="state">The flow.</param>
    /// <returns>The response.</returns>
    public static AuthenticationFlowResponse From(AuthenticationFlowState state)
        => new()
        {
            Name = state.Name,
            Description = state.Description,
            IsEnabled = state.IsEnabled,
            IsServing = state.IsServing,
            Problem = state.Problem,
            AuthenticationMethods = state.AuthenticationMethods,
            RequiredPermissions = state.RequiredPermissions,
            CanChangeRequiredPermission = state.CanChangeRequiredPermission,
            CanChangeRequiredPermissionReason = state.CanChangeRequiredPermissionReason,
        };
}
