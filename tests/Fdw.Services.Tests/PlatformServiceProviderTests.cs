using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;
using Fdw.Services.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests;

[Collection(nameof(ServicesTestCollection))]
public class DefaultServiceProviderTests
{
    private readonly TestServiceProvider _provider;
    private readonly Mock<IDomainConfigurationProvider<TestConfiguration>> _mockConfigProvider;
    private readonly Mock<IServiceFactory<IGenericService>> _mockFactory;

    public DefaultServiceProviderTests()
    {
        var logger = NullLogger<PlatformServiceProviderBase<IGenericService, TestConfiguration, IServiceFactory<IGenericService>, IDomainConfigurationProvider<TestConfiguration>>>.Instance;
        _provider = new TestServiceProvider(new ServiceCollection().BuildServiceProvider(), logger);
        _mockConfigProvider = new Mock<IDomainConfigurationProvider<TestConfiguration>>();
        _mockFactory = new Mock<IServiceFactory<IGenericService>>();

        // Why: PlatformServiceProviderBase.Get(name/id) now requires a parent provider for
        // O(1) name-to-type resolution. Register _mockConfigProvider as the parent.
        _provider.Register(_mockConfigProvider.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterFactoryReturnsSuccess()
    {
        var result = _provider.Register("TestType", _mockFactory.Object);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void RegisterConfigProviderReturnsSuccess()
    {
        var result = _provider.Register(_mockConfigProvider.Object);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameWithNoConfigurationsReturnsFailure()
    {
        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        // Why: CreateFromType calls Get(Guid) first; catch-all prevents NRE from unmocked async.
        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        _provider.Register(_mockConfigProvider.Object);

        var result = await _provider.Get("NonExistent", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameWithMatchingConfigAndFactoryReturnsService()
    {
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) after resolving name → ServiceOptionType.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockService.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameWhenFactoryCreateFailsReturnsFailure()
    {
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Failure(new GenericMessage("Creation failed")));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdWithMatchingConfigAndFactoryReturnsService()
    {
        var id = Guid.NewGuid();
        var config = new TestConfiguration { Id = id, Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(mockService.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdWithNoConfigurationsReturnsFailure()
    {
        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        _provider.Register(_mockConfigProvider.Object);

        var result = await _provider.Get(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdWhenFactoryCreateFailsReturnsFailure()
    {
        var id = Guid.NewGuid();
        var config = new TestConfiguration { Id = id, Name = "MyService", ServiceOptionType = "TestType" };
        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Failure(new GenericMessage("Creation failed")));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetTypedByNameWithCompatibleCastReturnsTypedService()
    {
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        var testService = new TestService(NullLogger<TestService>.Instance, config);

        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));

        // We need a factory that returns IGenericService which is also TestService
        var mockFactory = new Mock<IServiceFactory<IGenericService>>();
        mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(testService));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", mockFactory.Object);

        var result = await ((IPlatformServiceProvider)_provider).Get<TestService>("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(testService);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetTypedByNameWithIncompatibleCastReturnsFailure()
    {
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        // Try to cast to a type it doesn't implement
        var result = await ((IPlatformServiceProvider)_provider).Get<TestService>("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetTypedByIdWithCompatibleCastReturnsTypedService()
    {
        var id = Guid.NewGuid();
        var config = new TestConfiguration { Id = id, Name = "MyService", ServiceOptionType = "TestType" };
        var testService = new TestService(NullLogger<TestService>.Instance, config);

        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));

        var mockFactory = new Mock<IServiceFactory<IGenericService>>();
        mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(testService));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", mockFactory.Object);

        var result = await ((IPlatformServiceProvider)_provider).Get<TestService>(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(testService);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetTypedByIdWithIncompatibleCastReturnsFailure()
    {
        var id = Guid.NewGuid();
        var config = new TestConfiguration { Id = id, Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register(_mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await ((IPlatformServiceProvider)_provider).Get<TestService>(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task RegisterOverwritesExistingFactory()
    {
        var mockFactory1 = new Mock<IServiceFactory<IGenericService>>();
        var mockFactory2 = new Mock<IServiceFactory<IGenericService>>();

        _provider.Register("TestType", mockFactory1.Object);
        _provider.Register("TestType", mockFactory2.Object);

        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        mockFactory2.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register(_mockConfigProvider.Object);

        var result = await _provider.Get("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        mockFactory2.Verify(f => f.Create(It.IsAny<IGenericConfiguration>()), Times.Once);
    }}
