using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A configured sign-in flow as an administrator sees it: whether it runs, what it proves, and who it admits.
/// </summary>
/// <remarks>
/// Unlike <see cref="AuthenticationFlow"/>, which only ever describes a flow that loaded and runs, this
/// describes every configured flow, including one switched off and one whose configuration is broken,
/// because those are exactly the flows an administrator needs to see.
/// </remarks>
public sealed record AuthenticationFlowState
{
    /// <summary>Gets the flow's name, which is what a caller selects.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the flow's description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets a value indicating whether an operator has left the flow switched on.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Gets a value indicating whether this host is serving the flow right now.</summary>
    /// <remarks>False for a flow switched off, and for one switched on whose configuration failed to load.</remarks>
    public required bool IsServing { get; init; }

    /// <summary>Gets why a switched-on flow is not being served; null when it is served or switched off.</summary>
    public string? Problem { get; init; }

    /// <summary>Gets the RFC 8176 methods the flow's steps prove, e.g. <c>pwd</c>.</summary>
    public required IReadOnlyList<string> AuthenticationMethods { get; init; }

    /// <summary>Gets the permissions a principal must hold to be admitted; empty when every active user is.</summary>
    public required IReadOnlyList<string> RequiredPermissions { get; init; }

    /// <summary>Gets a value indicating whether a single required permission can be set or removed on this flow.</summary>
    public required bool CanChangeRequiredPermission { get; init; }

    /// <summary>Gets why <see cref="CanChangeRequiredPermission"/> is false, as a sentence to show the person.</summary>
    public string? CanChangeRequiredPermissionReason { get; init; }
}
