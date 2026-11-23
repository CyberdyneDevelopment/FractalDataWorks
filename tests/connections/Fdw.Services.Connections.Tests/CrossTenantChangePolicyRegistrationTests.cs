using System;
using System.Linq;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-4459: the connection and data store domains ask the cross-tenant change policy, which only the Settings service type registers. A host that registered the domain without it
/// is refused at initialization with the domain's own coded message, not with a bare container exception from whichever option resolves the provider first.
/// </summary>
public sealed class CrossTenantChangePolicyRegistrationTests
{
    private const bool Force = true;

    private static HostApplicationBuilder Builder()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddSingleton<IConfigurationGatewayProvider>(new ConfigurationGatewayProvider());
        return builder;
    }

    private sealed class PermitEverything : ICrossTenantChangePolicy
    {
        public IGenericResult Permit(string changeName, Guid ownerTenantId, Guid? actorTenantId) => GenericResult.Success();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostWithConnectionsAndNoPolicyIsRefusedAtInitializeWithTheConnectionDomainsOwnCode()
    {
        var builder = Builder();
        ConnectionTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        ConnectionTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        var initialized = ConnectionTypes.Initialize(host, NullLoggerFactory.Instance, Force);

        // Every missing service is reported: the policy and, since FDW-6208, the descriptions writer.
        initialized.IsFailure.ShouldBeTrue();
        var policy = initialized.Messages.Single(message => message.Code == "CONNECTIONS-60340");
        policy.Message.ShouldContain("ICrossTenantChangePolicy");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostWithConnectionsThePolicyAndTheDescriptionsServicesInitializes()
    {
        var builder = Builder();
        builder.Services.AddSingleton<ICrossTenantChangePolicy, PermitEverything>();
        builder.Services.AddSingleton(new Moq.Mock<Fdw.Services.Descriptions.IDescriptionWriter>(Moq.MockBehavior.Strict).Object);
        builder.Services.AddSingleton(new Moq.Mock<Fdw.Services.Descriptions.Abstractions.IDescriptionReader>(Moq.MockBehavior.Strict).Object);
        ConnectionTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        ConnectionTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        var initialized = ConnectionTypes.Initialize(host, NullLoggerFactory.Instance, Force);

        initialized.IsSuccess.ShouldBeTrue(initialized.CurrentMessage);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostWithConnectionsAndThePolicyButNoDescriptionsWriterIsRefusedAtInitializeWithItsOwnCode()
    {
        var builder = Builder();
        builder.Services.AddSingleton<ICrossTenantChangePolicy, PermitEverything>();
        ConnectionTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        ConnectionTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        var initialized = ConnectionTypes.Initialize(host, NullLoggerFactory.Instance, Force);

        initialized.IsFailure.ShouldBeTrue();
        var refused = initialized.Messages.ShouldHaveSingleItem();
        refused.Code.ShouldBe("CONNECTIONS-60381");
        refused.Message.ShouldContain("IDescriptionWriter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostWithDataStoresAndNoPolicyIsRefusedAtInitializeWithTheDataDomainsOwnCode()
    {
        var builder = Builder();
        DataStoreServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        DataStoreServiceTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        var initialized = DataStoreServiceTypes.Initialize(host, NullLoggerFactory.Instance, Force);

        // Every missing service is reported (FDW-4588): the policy and, since FDW-6121, the descriptions writer.
        initialized.IsFailure.ShouldBeTrue();
        var policy = initialized.Messages.Single(message => message.Code == "DATA-60341");
        policy.Message.ShouldContain("ICrossTenantChangePolicy");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostWithDataStoresAndThePolicyButNoDescriptionsWriterIsRefusedAtInitializeWithItsOwnCode()
    {
        var builder = Builder();
        builder.Services.AddSingleton<ICrossTenantChangePolicy, PermitEverything>();
        DataStoreServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        DataStoreServiceTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        var initialized = DataStoreServiceTypes.Initialize(host, NullLoggerFactory.Instance, Force);

        initialized.IsFailure.ShouldBeTrue();
        var refused = initialized.Messages.ShouldHaveSingleItem();
        refused.Code.ShouldBe("DATA-60342");
        refused.Message.ShouldContain("IDescriptionWriter");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Configuration")]
    public void AHostWithDataStoresThePolicyAndTheDescriptionsWriterPassesTheRequirements()
    {
        var builder = Builder();
        builder.Services.AddSingleton<ICrossTenantChangePolicy, PermitEverything>();
        builder.Services.AddSingleton(new Moq.Mock<Fdw.Services.Descriptions.IDescriptionWriter>(Moq.MockBehavior.Strict).Object);
        DataStoreServiceTypes.Configure(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        DataStoreServiceTypes.Register(builder, NullLoggerFactory.Instance, Force).IsSuccess.ShouldBeTrue();
        using var host = builder.Build();

        var initialized = DataStoreServiceTypes.Initialize(host, NullLoggerFactory.Instance, Force);

        initialized.Messages.ShouldNotContain(message => message.Code == "DATA-60341" || message.Code == "DATA-60342");
    }
}
