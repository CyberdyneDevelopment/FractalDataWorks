using System;
using System.Linq;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Endpoints;

namespace Fdw.Services.Identity.Tests;

/// <summary>
/// Guards the one property of the verify endpoint that matters most: it reports whether an identity
/// works without handing back the credential that makes it work.
/// </summary>
public class VerifyIdentityResponseTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void VerifyResponseCarriesNoTokenValue()
    {
        // Structural, not behavioural, and deliberately so: the response cannot leak a token if it has
        // nowhere to put one. This fails the moment someone adds a convenient "Token" property, which
        // is exactly when a reviewer most needs to be stopped — the endpoint is authenticated, so the
        // leak would look harmless.
        var members = typeof(VerifyIdentityResponse).GetProperties().Select(p => p.Name).ToArray();

        members.ShouldNotContain(nameof(IssuedIdentityToken.Value));
        members.ShouldNotContain("Token");
        members.ShouldNotContain("AccessToken");
        members.ShouldNotContain(nameof(IssuedIdentityToken.AuthorizationHeaderValue));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public void VerifyResponseCarriesWhatDiagnosesAFailure()
    {
        // The counterpart: withholding the token is only acceptable because everything an operator
        // needs to diagnose the failure IS present.
        var members = typeof(VerifyIdentityResponse).GetProperties().Select(p => p.Name).ToArray();

        members.ShouldContain(nameof(VerifyIdentityResponse.Issuer));
        members.ShouldContain(nameof(VerifyIdentityResponse.Audience));
        members.ShouldContain(nameof(VerifyIdentityResponse.GrantedScopes));
        members.ShouldContain(nameof(VerifyIdentityResponse.ExpiresAt));
        members.ShouldContain(nameof(VerifyIdentityResponse.Failure));
    }
}
