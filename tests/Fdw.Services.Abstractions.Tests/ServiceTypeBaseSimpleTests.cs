using Microsoft.Extensions.Logging;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Abstractions.Tests;

public class ServiceTypeBaseSimpleTests
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
    private class SimpleServiceType : ServiceTypeBase<SimpleService, SimpleFactory, SimpleConfig>
    {
        public SimpleServiceType()
            : base("Simple", "SimpleSection", "Simple Service", "Simple description",
                   "SimpleCategory")
        {
        Registration((builder, loggerFactory) =>
        {

                // Inline, because binding from appsettings is three lines in the body that wants it
                // rather than a helper on every service type that does not.
                builder.Services.AddOptions<SimpleConfig>()
                    .BindConfiguration(SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        }

    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void NameIsSetFromConstructor()
    {
        // Act
        var serviceType = new SimpleServiceType();

        // Assert
        serviceType.Name.ShouldBe("Simple");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ServiceTypePropertyReturnsServiceType()
    {
        // Act
        var serviceType = new SimpleServiceType();

        // Assert
        serviceType.ServiceType.ShouldBe(typeof(SimpleService));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigurationTypeReturnsConfigType()
    {
        // Act
        var serviceType = new SimpleServiceType();

        // Assert
        serviceType.ConfigurationType.ShouldBe(typeof(SimpleConfig));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void FactoryTypeReturnsFactoryType()
    {
        // Act
        var serviceType = new SimpleServiceType();

        // Assert
        serviceType.FactoryType.ShouldBe(typeof(SimpleFactory));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void SectionNameEqualsConfigurationKey()
    {
        // Act
        var serviceType = new SimpleServiceType();

        // Assert
        serviceType.SectionName.ShouldBe(serviceType.ConfigurationKey);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterWithNullLoggerWorks()
    {
        // Arrange
        var serviceType = new SimpleServiceType();
        var builder = Host.CreateApplicationBuilder();

        // Act
        var result = serviceType.Register(builder, null);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterWithLoggerWorks()
    {
        // Arrange
        var serviceType = new SimpleServiceType();
        var builder = Host.CreateApplicationBuilder();
        var loggerFactory = new NullLoggerFactory();

        // Act
        var result = serviceType.Register(builder, loggerFactory);

        // Assert
        result.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigureWithNullLoggerWorks()
    {
        // Arrange
        var serviceType = new SimpleServiceType();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act & Assert
        Should.NotThrow(() => serviceType.Configure(Host.CreateApplicationBuilder()));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void ConfigureWithLoggerWorks()
    {
        // Arrange
        var serviceType = new SimpleServiceType();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        var loggerFactory = new NullLoggerFactory();

        // Act & Assert
        Should.NotThrow(() => serviceType.Configure(Host.CreateApplicationBuilder()));
    }
}
