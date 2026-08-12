using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Clients.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Hosting.UiHost.Authentication;

/// <summary>
/// The two routes that turn an API token into a Blazor cookie, and back.
/// </summary>
/// <remarks>
/// A Blazor Server skin cannot authenticate against an FDW API from a component: the SignalR
/// circuit has no HttpContext to sign in against, so the exchange has to happen on a real request.
/// These two routes are that request, and the sequence is identical in every skin — post the
/// OpenIddict password grant, read the token, build a principal from its claims, store the tokens
/// on the cookie. Only <see cref="CookieSignInOptions"/> differs between deployments.
///
/// Minimal-API handlers rather than FastEndpoints classes because these are form posts from a login
/// page, not part of the API surface: a skin has no other reason to take FastEndpoints, and they are
/// never described in an OpenAPI document.
/// </remarks>
public static class CookieSignInRoutes
{
    /// <summary>Maps the sign-in and sign-out routes.</summary>
    /// <param name="routes">The route builder.</param>
    /// <param name="options">The values this deployment supplies.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public static void Map(IEndpointRouteBuilder routes, CookieSignInOptions options, ILoggerFactory? loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(routes);
        ArgumentNullException.ThrowIfNull(options);

        // Why DisableAntiforgery: the login form is posted by an unauthenticated caller who has no
        // antiforgery token yet, which is the one place the check cannot apply.
        routes.MapPost("/auth/login", (HttpContext context, IHttpClientFactory clients) =>
            SignIn(context, clients, options, loggerFactory)).DisableAntiforgery();

        routes.MapGet("/auth/logout", (HttpContext context, IHttpClientFactory clients) =>
            SignOut(context, clients, options, loggerFactory));
    }

    private static async Task SignIn(
        HttpContext context,
        IHttpClientFactory clients,
        CookieSignInOptions options,
        ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger("Fdw.Hosting.CookieSignIn") ?? NullLogger.Instance;
        var form = await context.Request.ReadFormAsync().ConfigureAwait(false);
        var username = form["username"].ToString();
        var returnUrl = Declared(form["returnUrl"].ToString(), options);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(form["password"].ToString()))
        {
            Redirect(context, options, returnUrl, "invalid-credentials");
            return;
        }

        var request = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = form["password"].ToString(),
            ["client_id"] = options.ClientId,
            ["scope"] = options.Scope,
        };

        // Forwarded so a multi-tenant user gets a tenant-scoped token rather than their default.
        if (!string.IsNullOrWhiteSpace(form["tenant"].ToString()))
        {
            request["tenant"] = form["tenant"].ToString();
        }

        var response = await clients.CreateClient(options.ClientName)
            .PostAsync("connect/token", new FormUrlEncodedContent(request)).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            CookieSignInLog.SignInRejected(logger, username, (int)response.StatusCode);
            Redirect(context, options, returnUrl, "invalid-credentials");
            return;
        }

        // OpenIddict answers in RFC 6749 snake_case.
        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }).ConfigureAwait(false);

        // Why both checks: a success status with an empty body deserializes to null, and a body in
        // an unrecognised shape deserializes with no access token. Both mean the server answered
        // something this client cannot use, and both were silent before they were logged.
        if (token is null)
        {
            CookieSignInLog.SignInEmptyResponse(logger, username, (int)response.StatusCode);
            Redirect(context, options, returnUrl, "invalid-credentials");
            return;
        }

        if (string.IsNullOrEmpty(token.AccessToken))
        {
            CookieSignInLog.SignInNoAccessToken(logger, username);
            Redirect(context, options, returnUrl, "invalid-credentials");
            return;
        }

        var expiry = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,

            // The cookie deliberately outlives the token: the validation hook refreshes the token
            // during a session, and a cookie expiring with it would end the session at the first
            // refresh instead.
            ExpiresUtc = expiry.AddDays(options.CookieLifetimeDays),
        };

        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = token.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = token.RefreshToken },
            new AuthenticationToken { Name = "expires_at", Value = expiry.ToString("o") },
        ]);

        await context.SignInAsync(
            options.Scheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken).Claims, "jwt", "name", "role")),
            properties).ConfigureAwait(false);

        CookieSignInLog.SignInSucceeded(logger, username);

        context.Response.Redirect(returnUrl);
    }

    private static async Task SignOut(
        HttpContext context,
        IHttpClientFactory clients,
        CookieSignInOptions options,
        ILoggerFactory? loggerFactory)
    {
        var logger = loggerFactory?.CreateLogger("Fdw.Hosting.CookieSignIn") ?? NullLogger.Instance;

        // Best-effort: the server revokes the refresh token so it cannot be replayed, but a failure
        // here must not leave the caller signed in locally, which is why the cookie is cleared
        // regardless of what the server said.
        var accessToken = await context.GetTokenAsync("access_token").ConfigureAwait(false);
        if (!string.IsNullOrEmpty(accessToken))
        {
            var client = clients.CreateClient(options.ClientName);
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            await client.PostAsync("auth/logout", null).ConfigureAwait(false);
        }

        await context.SignOutAsync(options.Scheme).ConfigureAwait(false);
        CookieSignInLog.SignedOut(logger);
        context.Response.Redirect("/");
    }

    private static void Redirect(HttpContext context, CookieSignInOptions options, string returnUrl, string error)
    {
        // LoginPath is a deployment value; returnUrl arrives already reduced to a resolved local
        // path and is escaped into the query string.
        context.Response.Redirect(
            options.LoginPath
            + $"?error={Uri.EscapeDataString(error)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    /// <summary>
    /// Resolves a caller-supplied return URL to a declared path.
    /// </summary>
    /// <remarks>
    /// Returns the matching entry from <see cref="CookieSignInOptions.ReturnPaths"/>, or the root.
    /// The returned string is always one the deployment declared, so nothing a caller wrote ever
    /// reaches a redirect - which is both the strongest form of the check and the only one a taint
    /// tracker can confirm, since there is no sanitiser to recognise.
    /// </remarks>
    /// <param name="returnUrl">The caller-supplied value.</param>
    /// <param name="options">The declared paths.</param>
    /// <returns>A declared path.</returns>
    private static string Declared(string? returnUrl, CookieSignInOptions options)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            return "/";
        }

        foreach (var candidate in options.ReturnPaths)
        {
            if (string.Equals(candidate, returnUrl, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return "/";
    }
}
