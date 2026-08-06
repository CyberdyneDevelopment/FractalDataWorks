using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Abstractions;
using Fdw.Services.Tests.TestHelpers;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Tests;

/// <summary>
/// Tests for DefaultServiceProvider explicit IFdwServiceProvider interface implementations
/// that are not covered by the typed generic Get&lt;T&gt; tests.
/// </summary>
[Collection(nameof(ServicesTestCollection))]
public class DefaultServiceProviderExplicitInterfaceTests
{
    private readonly DefaultServiceProvider<IGenericService, TestConfiguration, IServiceFactory<IGenericService>, IServiceConfigurationProvider<TestConfiguration>> _provider;
    private readonly Mock<IServiceConfigurationProvider<TestConfiguration>> _mockConfigProvider;
    private readonly Mock<IServiceFactory<IGenericService>> _mockFactory;

    public DefaultServiceProviderExplicitInterfaceTests()
    {
        var logger = NullLogger<DefaultServiceProvider<IGenericService, TestConfiguration, IServiceFactory<IGenericService>, IServiceConfigurationProvider<TestConfiguration>>>.Instance;
        _provider = new DefaultServiceProvider<IGenericService, TestConfiguration, IServiceFactory<IGenericService>, IServiceConfigurationProvider<TestConfiguration>>(new ServiceCollection().BuildServiceProvider(), logger);
        _mockConfigProvider = new Mock<IServiceConfigurationProvider<TestConfiguration>>();
        _mockFactory = new Mock<IServiceFactory<IGenericService>>();

        // Why: DefaultServiceProvider.Get(name/id) now requires a parent provider for
        // O(1) name-to-type resolution. Register _mockConfigProvider as the parent.
        _provider.RegisterParentProvider(_mockConfigProvider.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExplicitGetByNameWithCompatibleCastReturnsSuccess()
    {
        // Access via the non-generic IFdwServiceProvider interface
        IFdwServiceProvider explicitProvider = _provider;

        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        var testService = new TestService(NullLogger<TestService>.Instance, config);

        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));

        var mockFactory = new Mock<IServiceFactory<IGenericService>>();
        mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(testService));

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", mockFactory.Object);

        var result = await explicitProvider.Get<TestService>("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(testService);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExplicitGetByNameWithIncompatibleCastReturnsFailure()
    {
        IFdwServiceProvider explicitProvider = _provider;

        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await explicitProvider.Get<TestService>("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExplicitGetByNameWhenNotFoundReturnsFailure()
    {
        IFdwServiceProvider explicitProvider = _provider;

        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        // Why: CreateFromType calls Get(Guid) first; catch-all prevents NRE from unmocked async.
        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        _provider.Register("TestType", _mockConfigProvider.Object);

        var result = await explicitProvider.Get<TestService>("NonExistent", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExplicitGetByIdWithCompatibleCastReturnsSuccess()
    {
        IFdwServiceProvider explicitProvider = _provider;
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

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", mockFactory.Object);

        var result = await explicitProvider.Get<TestService>(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(testService);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExplicitGetByIdWithIncompatibleCastReturnsFailure()
    {
        IFdwServiceProvider explicitProvider = _provider;
        var id = Guid.NewGuid();

        var config = new TestConfiguration { Id = id, Name = "MyService", ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await explicitProvider.Get<TestService>(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task ExplicitGetByIdWhenNotFoundReturnsFailure()
    {
        IFdwServiceProvider explicitProvider = _provider;

        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        _provider.Register("TestType", _mockConfigProvider.Object);

        var result = await explicitProvider.Get<TestService>(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdWithNullNameConfigStillSucceeds()
    {
        var id = Guid.NewGuid();
        // Config that does not implement IGenericConfiguration Name property explicitly
        var config = new TestConfiguration { Id = id, Name = null!, ServiceOptionType = "TestType" };
        var mockService = new Mock<IGenericService>();

        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: Get(id) resolves name via parentConfig.Name ?? id.ToString(), then CreateFromType
        // calls configProvider.Get(name, TestContext.Current.CancellationToken). With null Name, the id string is used as name.
        _mockConfigProvider.Setup(cp => cp.Get(id.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Success(mockService.Object));

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByNameWithFactoryCreateFailureReturnsCorrectMessages()
    {
        var config = new TestConfiguration { Id = Guid.NewGuid(), Name = "MyService", ServiceOptionType = "TestType" };
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        // Why: CreateFromType calls Get(config.Id) first; must be set up to avoid NRE.
        _mockConfigProvider.Setup(cp => cp.Get(config.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Failure(new GenericMessage("Creation failed due to X")));

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get("MyService", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetByIdWithFactoryCreateFailureReturnsCorrectMessages()
    {
        var id = Guid.NewGuid();
        var config = new TestConfiguration { Id = id, Name = "MyService", ServiceOptionType = "TestType" };
        _mockConfigProvider.Setup(cp => cp.Get(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockConfigProvider.Setup(cp => cp.Get("MyService", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(config));
        _mockFactory.Setup(f => f.Create(It.IsAny<IGenericConfiguration>()))
            .Returns(GenericResult<IGenericService>.Failure(new GenericMessage("Creation failed due to X")));

        _provider.Register("TestType", _mockConfigProvider.Object);
        _provider.Register("TestType", _mockFactory.Object);

        var result = await _provider.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetTypedByNamePassesThroughFailureFromGet()
    {
        _mockConfigProvider.Setup(cp => cp.Get("Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        // Why: CreateFromType calls Get(Guid) first; catch-all prevents NRE from unmocked async.
        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        _provider.Register("TestType", _mockConfigProvider.Object);

        var result = await ((IFdwServiceProvider)_provider).Get<TestService>("Missing", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task GetTypedByIdPassesThroughFailureFromGet()
    {
        _mockConfigProvider.Setup(cp => cp.Get(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestConfiguration>.Success(default(TestConfiguration)!));
        _provider.Register("TestType", _mockConfigProvider.Object);

        var result = await ((IFdwServiceProvider)_provider).Get<TestService>(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
