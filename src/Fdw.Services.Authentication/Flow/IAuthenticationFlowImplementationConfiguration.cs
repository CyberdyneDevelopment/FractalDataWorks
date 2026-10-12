using System;
using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>The contract every AuthenticationFlow implementation carries.</summary>
public interface IAuthenticationFlowImplementationConfiguration : IImplementationConfiguration
{


    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the audience a token issued by this flow is for.</summary>
    string Audience { get; set; }

    /// <summary>Gets or sets the least authentication-context class this flow accepts.</summary>
    string? MinimumAcr { get; set; }

    /// <summary>Gets or sets how long one execution of this flow lives.</summary>
    string ExecutionLifetime { get; set; }

    /// <summary>
    /// Gets or sets the secret manager holding the key that encrypts this flow's stored state (a
    /// suspended execution, the token behind a one-time browser code). Null, with
    /// <see cref="SecretKeyName"/>, when the flow never stores state.
    /// </summary>
    string? SecretManagerName { get; set; }

    /// <summary>Gets or sets the key's name within <see cref="SecretManagerName"/>; null exactly when it is.</summary>
    string? SecretKeyName { get; set; }

    /// <summary>
    /// Gets or sets the absolute URLs a redirect sign-in may return the browser to, whitespace-separated
    /// and matched exactly. Null when the flow never returns a browser.
    /// </summary>
    string? ReturnUrls { get; set; }

    /// <summary>
    /// Gets or sets how long the one-time code this flow hands the browser stays redeemable, as an
    /// ISO-8601 duration. Set exactly when <see cref="ReturnUrls"/> is; null for a flow that returns
    /// no browser.
    /// </summary>
    string? CodeLifetime { get; set; }

    /// <summary>
    /// Gets or sets the Session authentication service this flow issues a server-side session into;
    /// null when the flow returns a bearer token (FDW-903).
    /// </summary>
    string? SessionServiceName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether callers may use this flow. False switches the flow off
    /// without deleting its row: selecting it is refused as disabled, it is not offered, and an
    /// execution of it already suspended cannot resume.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>The flow's steps, which hang from this row and are loaded with it.</summary>
    IList<AuthenticationFlowStepConfiguration> Steps { get; set; }
}
