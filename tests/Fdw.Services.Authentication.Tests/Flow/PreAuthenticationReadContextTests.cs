using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Binding;
using Fdw.Services.Authentication.Steps;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Authentication.Tests.Flow;

/// <summary>
/// Covers the FDW-owned classes a step's pre-authentication phase reads under system context:
/// <see cref="UserAccountEligibility"/>, <see cref="UserTenantResolver"/>, and
/// <see cref="ExternalIdentityBinding"/>. The two tests that exercised BakePermissionsStepType
/// directly moved with it to reference-servicetypes (FDW-848); these three classes stayed in FDW
/// because more than one step in more than one package reads them.
/// </summary>
public sealed class PreAuthenticationReadContextTests
{
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task AccountEligibilityReadsAsSystemWithoutBypassingAccountRules(bool active, bool notAMember, bool permitted)
    {
        var tenantId = Guid.NewGuid();
        var principal = new Principal { Id = Guid.NewGuid(), TenantId = tenantId };
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(p => p.Get(principal.Id, It.IsAny<CancellationToken>())).Returns(async (Guid _, CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();

            // The account's home tenant is deliberately a different one: a user may act in any tenant
            // they are a member of, so the home tenant is not what eligibility checks (FDW-825).
            return GenericResult<IUserImplementationConfiguration>.Success(new UserImplementationConfiguration
            {
                Id = principal.Id, TenantId = Guid.NewGuid(), IsActive = active,
            });
        });
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(p => p.ValidateTenantAccess(tenantId, principal.Id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<bool>.Success(!notAMember));

        var result = await new UserAccountEligibility(users.Object, accessor).MayBeIssued(principal, tenants.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Permitted.ShouldBe(permitted);
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Fact]
    public async Task TenantLookupRestoresAnonymousContext()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var accessor = new AuthenticationContextAccessor();
        var memberships = new Mock<IUserTenantConfigurationProvider>();
        memberships.Setup(p => p.Find(It.IsAny<Func<IUserTenantImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<IUserTenantImplementationConfiguration, bool> predicate, CancellationToken _) =>
            {
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();

                // The login lands in the DEFAULT membership, not the first one or the home tenant.
                IUserTenantImplementationConfiguration[] rows =
                [
                    new UserTenantImplementationConfiguration { UserId = userId, TenantId = Guid.NewGuid(), IsDefault = false },
                    new UserTenantImplementationConfiguration { UserId = userId, TenantId = tenantId, IsDefault = true },
                    new UserTenantImplementationConfiguration { UserId = Guid.NewGuid(), TenantId = Guid.NewGuid(), IsDefault = true },
                ];
                return Task.FromResult(GenericResult<IReadOnlyList<IUserTenantImplementationConfiguration>>.Success(
                    rows.Where(predicate).ToArray()));
            });

        var result = await new UserTenantResolver(memberships.Object, accessor).TenantFor(userId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(tenantId);
        accessor.Current.ShouldBeNull();
    }

    [Theory]
    [InlineData("trusted-issuer", "known-subject", true)]
    [InlineData("other-issuer", "known-subject", false)]
    [InlineData("trusted-issuer", "other-subject", false)]
    public async Task ExternalBindingRequiresExactIssuerAndSubjectUnderSystemContext(string issuer, string subject, bool bound)
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var accessor = new AuthenticationContextAccessor();
        var identities = new Mock<IExternalIdentityConfigurationProvider>();
        identities.Setup(p => p.Get(It.IsAny<CancellationToken>())).Returns(() =>
        {
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return Task.FromResult(GenericResult<IReadOnlyList<IExternalIdentityImplementationConfiguration>>.Success(
                [new ExternalIdentityConfiguration { UserId = userId, Issuer = "trusted-issuer", ExternalSubject = "known-subject", IsActive = true }]));
        });
        // The binding stamps a resolved row's first use, and that write runs under the same system
        // context the read does.
        identities.Setup(p => p.Save(
                It.IsAny<IExternalIdentityImplementationConfiguration>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                return Task.FromResult(GenericResult.Success());
            });

        var tenants = new Mock<ITenantResolver>();
        tenants.Setup(p => p.TenantFor(userId, It.IsAny<CancellationToken>())).Returns(() =>
        {
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return Task.FromResult(GenericResult<Guid>.Success(tenantId));
        });

        var result = await new ExternalIdentityBinding(identities.Object, tenants.Object, accessor)
            .Resolve(issuer, subject, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        (result.Value is not null).ShouldBe(bound);
        accessor.Current.ShouldBeNull();
        tenants.Verify(p => p.TenantFor(userId, It.IsAny<CancellationToken>()), bound ? Times.Once() : Times.Never());
        identities.Verify(
            p => p.Save(It.IsAny<IExternalIdentityImplementationConfiguration>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            bound ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task TenantLookupRestoresCallerAfterCancellation()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var memberships = new Mock<IUserTenantConfigurationProvider>();
        memberships.Setup(p => p.Find(It.IsAny<Func<IUserTenantImplementationConfiguration, bool>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<IUserTenantImplementationConfiguration, bool> _, CancellationToken _) =>
            {
                await Task.Yield();
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                throw new OperationCanceledException();
            });

        await Should.ThrowAsync<OperationCanceledException>(() => new UserTenantResolver(memberships.Object, accessor)
            .TenantFor(Guid.NewGuid(), TestContext.Current.CancellationToken));

        accessor.Current.ShouldBeSameAs(previous);
    }
}
