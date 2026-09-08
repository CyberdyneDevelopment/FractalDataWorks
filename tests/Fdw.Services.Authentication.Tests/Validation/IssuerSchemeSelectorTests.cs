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
    public void Select_rejects_a_bearer_value_that_is_not_three_segments()
        => IssuerSchemeSelector.Select(Request("Bearer not-a-jwt")).ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_rejects_a_token_whose_payload_is_not_readable()
        => IssuerSchemeSelector.Select(Request("Bearer aGVhZGVy.!!!not-base64!!!.c2ln"))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    [Fact]
    public void Select_rejects_a_token_that_names_no_issuer()
        => IssuerSchemeSelector.Select(Request("Bearer " + Segment("{}") + "." + Segment("{\"sub\":\"x\"}") + ".sig"))
            .ShouldBe(UnmatchedIssuerHandler.SchemeName);

    private static DefaultHttpContext Request(string? authorization)
    {
        var services = new ServiceCollection();
        // Through the registry, as Initialize fills it: the entries these bindings come from are
        // read through a gateway once the container is built, so they are never DI registrations.
        var bindings = new AuthenticationSchemeBindings();
        bindings.Add(new AuthenticationSchemeBinding(
            "FdwAuthority", FdwIssuer, "OpenIddict.Validation.AspNetCore"));
        bindings.Add(new AuthenticationSchemeBinding(
            "Partner", PartnerIssuer, "Fdw.JwtBearer.Partner"));
        services.AddSingleton(bindings);

        var context = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
        context.Request.Path = "/api/v1/etl/trigger/pipeline";
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;

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
