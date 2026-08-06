using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw.Abstractions;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Tests;

public sealed class ConnectionBaseTests
{
    private readonly Mock<IDataCommandTranslator<TestNativeCommand>> _mockTranslator;
    private readonly Mock<IDataContainer> _mockContainer;
    private readonly TestConnectionConfiguration _config;
    private readonly TestConnection _sut;

    public ConnectionBaseTests()
    {
        _mockTranslator = new Mock<IDataCommandTranslator<TestNativeCommand>>();
        _mockContainer = new Mock<IDataContainer>();
        _config = new TestConnectionConfiguration { Name = "TestConn" };
        _sut = new TestConnection(NullLogger<TestConnection>.Instance, _config, _mockTranslator.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericWithContainerReturnsFailureWhenTranslatorNotFound()
    {
        // Arrange
        var emptyTranslator = new Mock<IDataCommandTranslator<TestNativeCommand>>();
        emptyTranslator.Setup(t => t.Name).Returns("_Empty");

        var sut = new TestConnection(
            NullLogger<TestConnection>.Instance,
            _config,
            emptyTranslator.Object);

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await sut.Execute<object>(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericWithContainerReturnsFailureWhenTranslationFails()
    {
        // Arrange
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Failure(new GenericMessage("Translation failed")));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _sut.Execute<object>(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericWithContainerReturnsFailureWhenTranslationReturnsNullValue()
    {
        // Arrange
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Success(null!));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _sut.Execute<object>(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericWithContainerReturnsSuccessWhenTranslationSucceeds()
    {
        // Arrange
        var nativeCommand = new TestNativeCommand { Sql = "SELECT 1" };
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Success(nativeCommand));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _sut.Execute<string>(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("executed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteNonGenericWithContainerReturnsSuccessWhenExecutionSucceeds()
    {
        // Arrange
        var nativeCommand = new TestNativeCommand { Sql = "INSERT 1" };
        _mockTranslator.Setup(t => t.Name).Returns("Insert");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Success(nativeCommand));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Insert");

        // Act
        var result = await _sut.Execute(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteNonGenericWithContainerReturnsFailureWhenExecutionFails()
    {
        // Arrange
        var nativeCommand = new TestNativeCommand { Sql = "FAIL" };
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Success(nativeCommand));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        var failSut = new TestConnectionThatFails(NullLogger<TestConnectionThatFails>.Instance, _config, _mockTranslator.Object);

        // Act
        var result = await failSut.Execute(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteNonGenericWithContainerReturnsFailureWhenTranslatorEmpty()
    {
        // Arrange
        var emptyTranslator = new Mock<IDataCommandTranslator<TestNativeCommand>>();
        emptyTranslator.Setup(t => t.Name).Returns("_Empty");

        var sut = new TestConnection(
            NullLogger<TestConnection>.Instance,
            _config,
            emptyTranslator.Object);

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await sut.Execute(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteWithoutContainerGenericReturnsFailure()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        // Act
        var result = await _sut.Execute<object>(command.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteWithoutContainerNonGenericReturnsFailure()
    {
        // Arrange
        var command = new Mock<IDataCommand>();

        // Act
        var result = await _sut.Execute(command.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteNonGenericWithContainerReturnsFailureWhenTranslationFailsWithNoMessages()
    {
        // Arrange
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Failure());

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _sut.Execute(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteNonGenericWithContainerReturnsFailureWithMessagesWhenExecutionFailsWithMessages()
    {
        // Arrange
        var nativeCommand = new TestNativeCommand { Sql = "FAIL" };
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Success(nativeCommand));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        var failSut = new TestConnectionThatFailsWithMessages(
            NullLogger<TestConnectionThatFailsWithMessages>.Instance, _config, _mockTranslator.Object);

        // Act
        var result = await failSut.Execute(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericWithContainerForwardsTranslationMessagesWhenTranslationFailsWithMessages()
    {
        // Arrange - Translation fails WITH messages, so messages should be forwarded
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Failure(new GenericMessage("Specific translation error")));

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _sut.Execute<object>(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericWithContainerReturnsTranslationFailedWhenNoMessagesOnFailure()
    {
        // Arrange - Translation fails without messages, should get TranslationFailed message
        _mockTranslator.Setup(t => t.Name).Returns("Query");
        _mockTranslator
            .Setup(t => t.Translate(It.IsAny<IDataCommand>(), It.IsAny<IStorageContainer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<TestNativeCommand>.Failure());

        var command = new Mock<IDataCommand>();
        command.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _sut.Execute<object>(command.Object, _mockContainer.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ServiceTypeReturnsConcreteTypeName()
    {
        _sut.ServiceType.ShouldBe("TestConnection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void NameReturnsConfigurationName()
    {
        _sut.Name.ShouldBe("TestConn");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IdIsNotEmpty()
    {
        _sut.Id.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void IsAvailableIsTrueByDefault()
    {
        _sut.IsAvailable.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DisposeDoesNotThrow()
    {
        var connection = new TestConnection(NullLogger<TestConnection>.Instance, _config, _mockTranslator.Object);
        Should.NotThrow(() => connection.Dispose());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void DoubleDisposeDoesNotThrow()
    {
        var connection = new TestConnection(NullLogger<TestConnection>.Instance, _config, _mockTranslator.Object);
        connection.Dispose();
        Should.NotThrow(() => connection.Dispose());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteGenericViaServiceInterfaceReturnsFailureWhenCommandTypeMismatch()
    {
        // Arrange - Pass a non-IDataCommand IGenericCommand
        var nonDataCommand = new Mock<IGenericCommand>();
        nonDataCommand.Setup(c => c.CommandType).Returns("SomeCommand");

        // Act
        var result = await _sut.Execute<object>(nonDataCommand.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public async Task ExecuteNonGenericViaServiceInterfaceReturnsFailureWhenCommandTypeMismatch()
    {
        // Arrange - Pass a non-IDataCommand IGenericCommand
        var nonDataCommand = new Mock<IGenericCommand>();
        nonDataCommand.Setup(c => c.CommandType).Returns("SomeCommand");

        // Act
        var result = await _sut.Execute(nonDataCommand.Object, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    #region Test Doubles

    public class TestNativeCommand
    {
        public string Sql { get; set; } = string.Empty;
    }

    public class TestConnectionConfiguration : ConnectionConfiguration
    {
        public TestConnectionConfiguration() : base("Connection", "Test", "TestConnections")
        {
        }
    }

    public class TestConnection : ConnectionBase<TestNativeCommand, TestConnectionConfiguration, TestConnection>
    {
        private readonly IDataCommandTranslator<TestNativeCommand> _translator;

        public TestConnection(
            ILogger<TestConnection> logger,
            TestConnectionConfiguration configuration,
            IDataCommandTranslator<TestNativeCommand> translator)
            : base(logger, configuration)
        {
            _translator = translator;
        }

        protected override IDataCommandTranslator<TestNativeCommand> GetTranslator(string commandType)
            => _translator;

        protected override Task<IGenericResult<T>> Execute<T>(
            TestNativeCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken)
        {
            // Simulate successful execution
            if (typeof(T) == typeof(string))
            {
                return Task.FromResult(GenericResult<T>.Success((T)(object)"executed"));
            }
            return Task.FromResult(GenericResult<T>.Success(default(T)!));
        }

        protected override Task<IGenericResult> Execute(
            TestNativeCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult.Success());
        }
    }

    public class TestConnectionThatFails : ConnectionBase<TestNativeCommand, TestConnectionConfiguration, TestConnectionThatFails>
    {
        private readonly IDataCommandTranslator<TestNativeCommand> _translator;

        public TestConnectionThatFails(
            ILogger<TestConnectionThatFails> logger,
            TestConnectionConfiguration configuration,
            IDataCommandTranslator<TestNativeCommand> translator)
            : base(logger, configuration)
        {
            _translator = translator;
        }

        protected override IDataCommandTranslator<TestNativeCommand> GetTranslator(string commandType)
            => _translator;

        protected override Task<IGenericResult<T>> Execute<T>(
            TestNativeCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult<T>.Failure());
        }

        protected override Task<IGenericResult> Execute(
            TestNativeCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult.Failure());
        }
    }

    public class TestConnectionThatFailsWithMessages : ConnectionBase<TestNativeCommand, TestConnectionConfiguration, TestConnectionThatFailsWithMessages>
    {
        private readonly IDataCommandTranslator<TestNativeCommand> _translator;

        public TestConnectionThatFailsWithMessages(
            ILogger<TestConnectionThatFailsWithMessages> logger,
            TestConnectionConfiguration configuration,
            IDataCommandTranslator<TestNativeCommand> translator)
            : base(logger, configuration)
        {
            _translator = translator;
        }

        protected override IDataCommandTranslator<TestNativeCommand> GetTranslator(string commandType)
            => _translator;

        protected override Task<IGenericResult<T>> Execute<T>(
            TestNativeCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult<T>.Failure(new GenericMessage("Execution failed with message")));
        }

        protected override Task<IGenericResult> Execute(
            TestNativeCommand command,
            IStorageContainer container,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(GenericResult.Failure(new GenericMessage("Execution failed with message")));
        }
    }

    #endregion
}
