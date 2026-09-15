using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Flow.StepTypes;
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

public sealed class PreAuthenticationReadContextTests
{
    [Fact]
    public async Task PermissionBakingReadsTenantAsSystemAndRestoresContextOnCancellation()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var tenantId = Guid.NewGuid();
        var tenants = new Mock<ITenantProvider>();
        tenants.Setup(p => p.GetTenant(tenantId, It.IsAny<CancellationToken>()))
            .Returns(async (Guid _, CancellationToken _) =>
            {
                await Task.Yield();
                accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
                throw new OperationCanceledException();
            });
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<IAuthenticationContextAccessor>(accessor);
        builder.Services.AddSingleton(tenants.Object);
        builder.Services.AddSingleton(Mock.Of<IEffectivePermissionResolver>());
        using var host = builder.Build();
        var step = new BakePermissionsStepType();
        step.Initialize(host, null);
        var context = new AuthenticationContext
        {
            Principal = new Principal { Id = Guid.NewGuid(), TenantId = tenantId },
        };
        await Should.ThrowAsync<OperationCanceledException>(() => step.Execute(context, TestContext.Current.CancellationToken));
        accessor.Current.ShouldBeSameAs(previous);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public async Task AccountEligibilityReadsAsSystemWithoutBypassingAccountRules(bool active, bool wrongTenant, bool permitted)
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
            return GenericResult<IUserImplementationConfiguration>.Success(new UserImplementationConfiguration
            {
                Id = principal.Id, TenantId = wrongTenant ? Guid.NewGuid() : tenantId, IsActive = active,
            });
        });

        var result = await new UserAccountEligibility(users.Object, accessor).MayBeIssued(principal, TestContext.Current.CancellationToken);

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
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(p => p.Get(userId, It.IsAny<CancellationToken>())).Returns(() =>
        {
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            return Task.FromResult(GenericResult<IUserImplementationConfiguration>.Success(
                new UserImplementationConfiguration { Id = userId, TenantId = tenantId }));
        });

        var result = await new UserTenantResolver(users.Object, accessor).TenantFor(userId, TestContext.Current.CancellationToken);

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
                [new ExternalIdentityConfiguration { UserId = userId, Provider = "trusted-issuer", ExternalSubject = "known-subject", IsActive = true }]));
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
    }

    [Fact]
    public async Task TenantLookupRestoresCallerAfterCancellation()
    {
        var previous = Mock.Of<IAuthenticationContext>();
        var accessor = new AuthenticationContextAccessor { Current = previous };
        var users = new Mock<IUserConfigurationProvider>();
        users.Setup(p => p.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(async (Guid _, CancellationToken _) =>
        {
            await Task.Yield();
            accessor.Current.ShouldNotBeNull().IsSystemContext.ShouldBeTrue();
            throw new OperationCanceledException();
        });

        await Should.ThrowAsync<OperationCanceledException>(() => new UserTenantResolver(users.Object, accessor)
            .TenantFor(Guid.NewGuid(), TestContext.Current.CancellationToken));

        accessor.Current.ShouldBeSameAs(previous);
    }
}
