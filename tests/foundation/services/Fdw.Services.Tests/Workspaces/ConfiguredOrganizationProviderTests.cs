using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-4573: a host that runs the single-tenant type (the ETL host and the scheduler) can still find an organization, because a pipeline is made in one. It was the null provider,
/// which failed every lookup, so no pipeline could be created. A lookup that finds nothing says so with its own code, and one that cannot be made says that.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class ConfiguredOrganizationProviderTests
{
    private static readonly Guid OrganizationId = Guid.Parse("60ee45a2-8b39-46a0-9941-7dfdbe0fff21");
    private static readonly Guid TenantId = Guid.Parse("cd6d54b9-0ccf-46ab-9eb5-c28a8d6c0178");

    private static ConfiguredOrganizationProvider Provider(IGenericResult<IEnumerable<OrganizationRow>> answer)
    {
        var gateway = new Mock<IConfigurationGateway>();
        gateway
            .Setup(g => g.Execute<IEnumerable<OrganizationRow>>(It.IsAny<IDataCommand>(), It.IsAny<DataStoreTarget>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(answer);
        var gateways = new Mock<IConfigurationGatewayProvider>();
        gateways.Setup(g => g.Get(It.IsAny<string>())).Returns(GenericResult<IConfigurationGateway>.Success(gateway.Object));
        return new ConfiguredOrganizationProvider(gateways.Object, "ConfigurationDb", "tenant", "Organization", NullLogger<ConfiguredOrganizationProvider>.Instance);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task ASingleTenantHostRegistersARealOrganizationLookupAndNotTheNullOne()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton(Mock.Of<IConfigurationGatewayProvider>());
        builder.Services.AddLogging();

        new SingleTenantMultitenancyType().Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        using var provider = builder.Services.BuildServiceProvider();
        provider.GetRequiredService<IOrganizationProvider>().ShouldBeOfType<ConfiguredOrganizationProvider>();
        await Task.CompletedTask;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AnOrganizationTheStoreHoldsIsReadWithItsTenantAndItsActiveFlag()
    {
        var provider = Provider(GenericResult<IEnumerable<OrganizationRow>>.Success(
            [new OrganizationRow { Id = OrganizationId, TenantId = TenantId, Name = "AFC", Slug = "default", IsDefault = true, IsActive = true, IsCurrent = true }]));

        var result = await provider.Get(OrganizationId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.TenantId.ShouldBe(TenantId);
        result.Value.IsActive.ShouldBeTrue();
        result.Value.Name.ShouldBe("AFC");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AnOrganizationTheStoreDoesNotHoldIsNotFoundWithItsOwnCode()
    {
        var provider = Provider(GenericResult<IEnumerable<OrganizationRow>>.Success([]));

        var result = await provider.Get(OrganizationId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ORGLOOKUP-30001");
        FailureCategory.IsMissing(result).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public async Task AStoreThatCannotBeReadIsItsOwnCodeAndNotNotFound()
    {
        var provider = Provider(GenericResult<IEnumerable<OrganizationRow>>.Failure(new Fdw.Messages.GenericMessage("the store refused it")));

        var result = await provider.Get(OrganizationId, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages[^1].Code.ShouldBe("ORGLOOKUP-90001");
        FailureCategory.IsMissing(result).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Workspaces")]
    public async Task ATenantsDefaultOrganizationIsReadAndATenantWithNoneIsAnswered()
    {
        var found = Provider(GenericResult<IEnumerable<OrganizationRow>>.Success(
            [new OrganizationRow { Id = OrganizationId, TenantId = TenantId, IsDefault = true, IsActive = true, IsCurrent = true }]));
        var none = Provider(GenericResult<IEnumerable<OrganizationRow>>.Success([]));

        (await found.GetDefault(TenantId, TestContext.Current.CancellationToken)).Value!.Id.ShouldBe(OrganizationId);
        var missing = await none.GetDefault(TenantId, TestContext.Current.CancellationToken);

        missing.IsFailure.ShouldBeTrue();
        missing.Messages[^1].Code.ShouldBe("ORGLOOKUP-30002");
    }
}
