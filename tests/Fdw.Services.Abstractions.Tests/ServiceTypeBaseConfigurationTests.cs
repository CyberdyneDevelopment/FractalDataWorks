using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fdw.Services.Abstractions.Tests;

/// <summary>
/// Tests for ServiceTypeBase configuration registration
/// </summary>
public class ServiceTypeBaseConfigurationTests
{
    [ExcludeFromCodeCoverage]
    private class SimpleCommand : IGenericCommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string CommandType => "Simple";
        public string Category => "Test";
    }

    [ExcludeFromCodeCoverage]
    private class SimpleConfig : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "Test";
        public string SectionName => "Test";
        public string ServiceType => "Test";
        public string? ServiceOptionType => "Test";
    }

    [ExcludeFromCodeCoverage]
    private class SimpleService : IGenericService
    {
        public string Id => "test";
        public string ServiceType => "Test";
        public bool IsAvailable => true;

        public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult(GenericResult<T>.Success(default!));

        public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken)
            => Task.FromResult(GenericResult.Success());
    }

    [ExcludeFromCodeCoverage]
    private class SimpleFactory : IServiceFactory<SimpleService, SimpleConfig>
    {
        public IGenericResult<SimpleService> Create(SimpleConfig configuration)
            => GenericResult<SimpleService>.Success(new SimpleService());

        public IGenericResult<SimpleService> Create(IGenericConfiguration configuration)
            => GenericResult<SimpleService>.Success(new SimpleService());

        IGenericResult<IGenericService> IServiceFactory.Create(IGenericConfiguration configuration)
            => GenericResult<IGenericService>.Success(new SimpleService());

        IGenericResult<T> IServiceFactory.Create<T>(IGenericConfiguration configuration)
            => GenericResult<T>.Success((T)(IGenericService)new SimpleService());
    }

    [ExcludeFromCodeCoverage]
    private class ConfigurationTestServiceType : ServiceTypeBase<SimpleService, SimpleFactory, SimpleConfig>
    {
        public ConfigurationTestServiceType()
            : base("ConfigTest", "ConfigTestSection", "Config Test Service", "Test",
                   "Test")
        {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

                RegisterConfiguration(builder.Services);
                return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        }

        // Expose protected method for testing
        public void TestRegisterConfiguration(IServiceCollection services)
        {
            RegisterConfiguration(services);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RegisterConfigurationThrowsWhenServicesIsNull()
    {
        // Arrange
        var serviceType = new ConfigurationTestServiceType();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            serviceType.TestRegisterConfiguration(null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RegisterConfigurationAddsOptionsConfiguration()
    {
        // Arrange
        var serviceType = new ConfigurationTestServiceType();
        var services = new ServiceCollection();

        // Act
        serviceType.TestRegisterConfiguration(services);

        // Assert
        services.ShouldContain(sd => sd.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void RegisterCallsRegisterConfiguration()
    {
        // Arrange
        var serviceType = new ConfigurationTestServiceType();
        var builder = Host.CreateApplicationBuilder();

        // Act
        var result = serviceType.Register(builder, null, "TestStore", "TestPath", "TestContainer");

        // Assert
        result.ShouldNotBeNull();
        builder.Services.ShouldContain(sd => sd.ServiceType.Name.Contains("IConfigureOptions"));
    }
}
