using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Binding;
using Fdw.Services.Configuration;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// An operator's invitation binds a pre-created account to the first matching sign-in, once (FDW-910).
/// </summary>
[Trait("Category", "Security")]
public class InvitationRedemptionTests
{
    private const string Issuer = "https://login.example/tenant/v2.0";
    private static readonly Guid VendorId = new("7e7e7e7e-0000-0000-0000-000000000001");
    private static readonly Guid VendorTenant = new("7e7e7e7e-0000-0000-0000-000000000002");

    [Fact]
    public async Task AnOpenInvitationIsRedeemedByTheVerifiedAddressAndBecomesABinding()
    {
        var invitation = Invitation("vendor@partner.example");
        var (binding, identities) = Build(invitation);

        var result = await binding.RedeemInvitation(Issuer, "oid-123", "Vendor@Partner.example", TestContext.Current.CancellationToken);

        result.Value.ShouldNotBeNull().Id.ShouldBe(VendorId);
        result.Value.TenantId.ShouldBe(VendorTenant);
        invitation.ExternalSubject.ShouldBe("oid-123");
        invitation.BoundAt.ShouldNotBeNull();
        identities.Verify(i => i.Save(invitation, "ExternalIdentity", "ExternalIdentity", invitation.Name, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ARedeemedInvitationIsNeverMatchedByAddressAgain()
    {
        var redeemed = Invitation("vendor@partner.example");
        redeemed.ExternalSubject = "oid-123";
        var (binding, _) = Build(redeemed);

        var result = await binding.RedeemInvitation(Issuer, "oid-someone-else", "vendor@partner.example", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AnInvitationForAnotherIssuerDoesNotMatch()
    {
        var (binding, _) = Build(Invitation("vendor@partner.example"));

        var result = await binding.RedeemInvitation("https://other.example", "oid-123", "vendor@partner.example", TestContext.Current.CancellationToken);

        result.Value.ShouldBeNull();
    }

    [Fact]
    public async Task AnInvitationThatCannotBeWrittenRefusesTheSignIn()
    {
        var (binding, identities) = Build(Invitation("vendor@partner.example"));
        identities.Setup(i => i.Save(It.IsAny<IExternalIdentityImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(new Fdw.Messages.GenericMessage("write failed")));

        var result = await binding.RedeemInvitation(Issuer, "oid-123", "vendor@partner.example", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task ResolveNeverMatchesAnOpenInvitation()
    {
        // An open invitation has no subject; a subject-less row must not be what an exact lookup finds.
        var (binding, _) = Build(Invitation("vendor@partner.example"));

        var result = await binding.Resolve(Issuer, "oid-123", TestContext.Current.CancellationToken);

        result.Value.ShouldBeNull();
    }

    private static ExternalIdentityConfiguration Invitation(string email) => new()
    {
        Name = ExternalIdentityConfiguration.InvitationName(Issuer, email),
        Issuer = Issuer,
        ExternalSubject = null,
        InvitedEmail = email,
        UserId = VendorId,
        IsActive = true,
    };

    private static (ExternalIdentityBinding Binding, Mock<IExternalIdentityConfigurationProvider> Identities) Build(
        params ExternalIdentityConfiguration[] rows)
    {
        var identities = new Mock<IExternalIdentityConfigurationProvider>();
        identities.Setup(i => i.Get(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IReadOnlyList<IExternalIdentityImplementationConfiguration>>.Success(rows));
        identities.Setup(i => i.Save(It.IsAny<IExternalIdentityImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        var tenants = new Mock<ITenantResolver>();
        tenants.Setup(t => t.TenantFor(VendorId, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult<Guid>.Success(VendorTenant));
        return (new ExternalIdentityBinding(identities.Object, tenants.Object, new AuthenticationContextAccessor()), identities);
    }
}
