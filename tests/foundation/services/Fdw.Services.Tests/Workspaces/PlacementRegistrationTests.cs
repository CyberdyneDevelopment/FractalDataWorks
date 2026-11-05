using System.Linq;
using Fdw.Services.Configuration;
using Fdw.Services.Multitenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests.Workspaces;

/// <summary>
/// FDW-3692: the placement of a new member in its tenant's default workspace is registered by the multitenancy option every host runs, so
/// the ETL host and the scheduler, which run <c>SingleTenant</c>, can make a pipeline. A host that registered none refused every create.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public sealed class PlacementRegistrationTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Workspaces")]
    public void ASingleTenantHostRegistersThePlacementOnce()
    {
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        var option = new SingleTenantMultitenancyType();

        option.Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();
        option.Register(builder, NullLoggerFactory.Instance).IsSuccess.ShouldBeTrue();

        builder.Services.Count(descriptor => descriptor.ServiceType == typeof(IConfigurationPlacement)).ShouldBe(1);
    }
}
