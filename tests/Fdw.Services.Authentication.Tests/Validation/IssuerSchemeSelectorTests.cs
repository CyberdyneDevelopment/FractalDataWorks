using System;
using System.Text;
using Fdw.Services.Authentication.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Fdw.Services.Authentication.Tests.Validation;

/// <summary>
/// Tests for the per-request scheme selection — which validator a token is handed to, decided by the
/// issuer the token names.
/// </summary>
public sealed class IssuerSchemeSelectorTests
{
    private const string FdwIssuer = "https://internal-proj.example/";
    private const string PartnerIssuer = "https://idp.example/o/partner/";

    [Fact]
    public void Select_routes_a_token_to_the_scheme_declared_for_its_issuer()
        => IssuerSchemeSelector.Select(Request(TokenFor(FdwIssuer)))
            .ShouldBe("OpenIddict.Validation.AspNetCore");

    [Fact]
    public void Select_routes_a_second_issuer_to_its_own_scheme()
        => IssuerSchemeSelector.Select(Request(TokenFor(PartnerIssuer)))
            .ShouldBe("Fdw.JwtBearer.Partner");

    // An issuer nobody declared is not handed to some other issuer's validator on the chance it
    // works — that would report an undeclared issuer as that validator's rejection.
    [Fact]
    public void Select_rejects_an_issuer_no_authentication_service_declares()
        => IssuerSchemeSelector.Select(Request(TokenFor("https://stranger.example/")))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    // A trailing slash does not make a different issuer here. The minter reads
    // auth.JwtTokenManager.Issuer and the validator reads the declared Authority; each is free to
    // carry or omit the slash and nothing makes them agree, so comparing ordinally produced 401 on
    // every authenticated route twice in one day - once with the slash on the declared side, once
    // on the token side. Neither string is rewritten; the two are compared as issuers rather than
    // as bytes, and IssuerName.Spellings carries the same decision into ValidIssuers so the scheme
    // this selects then accepts the token it was selected for.
    [Fact]
    public void Select_routes_a_differently_slashed_issuer_to_the_same_scheme()
        => IssuerSchemeSelector.Select(Request(TokenFor("https://internal-proj.example")))
            .ShouldBe("OpenIddict.Validation.AspNetCore");

    [Fact]
    public void Select_rejects_a_request_with_no_authorization_header()
        => IssuerSchemeSelector.Select(Request(null)).ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_rejects_a_non_bearer_authorization_header()
        => IssuerSchemeSelector.Select(Request("Basic dXNlcjpwYXNz")).ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_rejects_a_bearer_value_that_is_not_three_segments_when_no_api_key_service_is_declared()
        => IssuerSchemeSelector.Select(Request("Bearer not-a-jwt")).ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_routes_a_bearer_value_that_is_not_a_jwt_to_the_declared_api_key_scheme()
        => IssuerSchemeSelector.Select(Request("Bearer fdx_agent_abc123", apiKey: true)).ShouldBe("Fdw.ApiKey.ApiKey");

    [Fact]
    public void Select_routes_an_opaque_credential_by_shape_not_by_a_fixed_prefix()
        // FDW-939: the prefix is the credential service's configuration, so any prefix routes.
        => IssuerSchemeSelector.Select(Request("Bearer acme_prod_abc123", apiKey: true)).ShouldBe("Fdw.ApiKey.ApiKey");

    [Fact]
    public void Select_still_routes_a_jwt_by_its_issuer_when_an_api_key_service_is_declared()
        => IssuerSchemeSelector.Select(Request(TokenFor(FdwIssuer), apiKey: true))
            .ShouldBe("OpenIddict.Validation.AspNetCore");

    [Fact]
    public void Select_does_not_route_an_empty_bearer_value_as_an_opaque_credential()
        => IssuerSchemeSelector.Select(Request("Bearer ", apiKey: true)).ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_rejects_a_token_whose_payload_is_not_readable()
        => IssuerSchemeSelector.Select(Request("Bearer aGVhZGVy.!!!not-base64!!!.c2ln"))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_rejects_a_token_that_names_no_issuer()
        => IssuerSchemeSelector.Select(Request("Bearer " + Segment("{}") + "." + Segment("{\"sub\":\"x\"}") + ".sig"))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_routes_a_request_with_no_header_by_the_session_cookie_it_carries()
        => IssuerSchemeSelector.Select(Request(null, "fdw_session=abc"))
            .ShouldBe("Fdw.Session.ConsoleSession");

    [Fact]
    public void Select_rejects_a_request_carrying_cookies_of_two_session_services()
        => IssuerSchemeSelector.Select(Request(null, "fdw_session=abc; fdw_other=def"))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_ignores_a_cookie_no_session_service_declares()
        => IssuerSchemeSelector.Select(Request(null, "unrelated=abc"))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_routes_by_the_authorization_header_when_a_session_cookie_is_also_present()
        => IssuerSchemeSelector.Select(Request(TokenFor(FdwIssuer), "fdw_session=abc"))
            .ShouldBe("OpenIddict.Validation.AspNetCore");

    private static DefaultHttpContext Request(string? authorization, string? cookie = null, bool apiKey = false)
    {
        var services = new ServiceCollection();
        // Through the registry, as Initialize fills it: the entries these bindings come from are
        // read through a gateway once the container is built, so they are never DI registrations.
        var bindings = new AuthenticationSchemeBindings();
        bindings.Add(new AuthenticationSchemeBinding(
            "FdwAuthority", FdwIssuer, "OpenIddict.Validation.AspNetCore"));
        bindings.Add(new AuthenticationSchemeBinding(
            "Partner", PartnerIssuer, "Fdw.JwtBearer.Partner"));
        bindings.Add(new AuthenticationSchemeBinding(
            "ConsoleSession", SessionAuthenticationType.SessionIssuer, "Fdw.Session.ConsoleSession", "fdw_session"));
        bindings.Add(new AuthenticationSchemeBinding(
            "OtherSession", SessionAuthenticationType.SessionIssuer, "Fdw.Session.OtherSession", "fdw_other"));
        if (apiKey)
            bindings.Add(new AuthenticationSchemeBinding(
                "ApiKey", ApiKeyAuthenticationType.OpaqueCredentialIssuer, ApiKeyAuthenticationType.SchemeNameFor("ApiKey")));
        services.AddSingleton(bindings);

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Path = "/api/v1/etl/trigger/pipeline";
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;
        if (cookie is not null)
            context.Request.Headers.Cookie = cookie;

        return context;
    }

    private static string TokenFor(string issuer)
        => "Bearer " + Segment("{\"alg\":\"RS256\"}")
            + "." + Segment("{\"iss\":\"" + issuer + "\",\"sub\":\"svc\"}")
            + ".signature";

    // Base64url, unpadded — the encoding a real token uses, so the decoder is exercised as it will be.
    private static string Segment(string json)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
