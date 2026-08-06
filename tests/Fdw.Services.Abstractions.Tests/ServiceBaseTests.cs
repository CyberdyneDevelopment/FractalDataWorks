using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Abstractions.Tests;

public class ServiceBaseTests
{
    [ExcludeFromCodeCoverage]
    private class TestCommand : IGenericCommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public string CommandType => "TestCommand";
        public string Category => "Test";
        public string Name { get; set; } = "TestCommand";
    }

    [ExcludeFromCodeCoverage]
    private class TestConfiguration : IGenericConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "TestConfig";
        public string SectionName => "Test";
        public string ServiceType => "Test";
        public string? ServiceOptionType => "Test";
    }

    [ExcludeFromCodeCoverage]
    private class TestService : ServiceBase<TestCommand, TestConfiguration, TestService>
    {
        public TestService(ILogger<TestService> logger, TestConfiguration configuration)
            : base(logger, configuration)
        {
        }

        public override Task<IGenericResult> Execute(TestCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult.Success());
        }

        public override Task<IGenericResult<T>> Execute<T>(TestCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult<T>.Success(default!));
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsId()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();

        // Act
        var service = new TestService(logger, config);

        // Assert
        service.Id.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorGeneratesUniqueIds()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();

        // Act
        var service1 = new TestService(logger, config);
        var service2 = new TestService(logger, config);

        // Assert
        service1.Id.ShouldNotBe(service2.Id);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorSetsServiceType()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();

        // Act
        var service = new TestService(logger, config);

        // Assert
        service.ServiceType.ShouldBe("TestService");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorThrowsWhenConfigurationIsNull()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new TestService(logger, null!));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void ConstructorAcceptsNullLogger()
    {
        // Arrange
        var config = new TestConfiguration();

        // Act
        var service = new TestService(null!, config);

        // Assert
        service.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void IsAvailableIsTrueByDefault()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();

        // Act
        var service = new TestService(logger, config);

        // Assert
        service.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NameReturnsConfigurationName()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration { Name = "MyService" };

        // Act
        var service = new TestService(logger, config);

        // Assert
        service.Name.ShouldBe("MyService");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void NameReturnsTypeNameWhenConfigurationNameIsNull()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration { Name = null! };

        // Act
        var service = new TestService(logger, config);

        // Assert
        service.Name.ShouldBe("TestService");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteWithIGenericCommandReturnsSuccessForMatchingType()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();
        var service = new TestService(logger, config);
        IGenericCommand command = new TestCommand();

        // Act
        var result = await service.Execute(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteWithIGenericCommandReturnsFailureForWrongType()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();
        var service = new TestService(logger, config);
        IGenericCommand command = Mock.Of<IGenericCommand>();

        // Act
        var result = await service.Execute(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericWithIGenericCommandReturnsSuccessForMatchingType()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();
        var service = new TestService(logger, config);
        IGenericCommand command = new TestCommand();

        // Act
        var result = await service.Execute<string>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ExecuteGenericWithIGenericCommandReturnsFailureForWrongType()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();
        var service = new TestService(logger, config);
        IGenericCommand command = Mock.Of<IGenericCommand>();

        // Act
        var result = await service.Execute<string>(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DisposeDoesNotThrow()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();
        var service = new TestService(logger, config);

        // Act & Assert
        Should.NotThrow(() => service.Dispose());
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DisposeCanBeCalledMultipleTimes()
    {
        // Arrange
        var logger = NullLogger<TestService>.Instance;
        var config = new TestConfiguration();
        var service = new TestService(logger, config);

        // Act & Assert
        Should.NotThrow(() =>
        {
            service.Dispose();
            service.Dispose();
        });
    }

    [ExcludeFromCodeCoverage]
    private class DisposableTestService : ServiceBase<TestCommand, TestConfiguration, DisposableTestService>
    {
        public bool WasDisposed { get; private set; }

        public DisposableTestService(ILogger<DisposableTestService> logger, TestConfiguration configuration)
            : base(logger, configuration)
        {
        }

        public override Task<IGenericResult> Execute(TestCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult.Success());
        }

        public override Task<IGenericResult<T>> Execute<T>(TestCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult<T>.Success(default!));
        }

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                WasDisposed = true;
            }
            base.Dispose(disposing);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public void DisposeCallsDisposeBoolWithTrue()
    {
        // Arrange
        var logger = NullLogger<DisposableTestService>.Instance;
        var config = new TestConfiguration();
        var service = new DisposableTestService(logger, config);

        // Act
        service.Dispose();

        // Assert
        service.WasDisposed.ShouldBeTrue();
    }
}
