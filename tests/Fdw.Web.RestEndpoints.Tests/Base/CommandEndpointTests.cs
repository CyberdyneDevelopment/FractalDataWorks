using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Web.RestEndpoints.Base;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Tests.Base;

// Test implementation for CommandEndpoint with result
public class TestCommandEndpoint : CommandEndpoint<TestCommand, TestCommandResult>
{
    private readonly Func<TestCommand, CancellationToken, Task<IGenericResult<TestCommandResult>>> _executeFunc;
    private readonly Func<TestCommand, CancellationToken, Task<IGenericResult>>? _authFunc;
    private readonly string[]? _requiredRoles;

    public TestCommandEndpoint(
        Func<TestCommand, CancellationToken, Task<IGenericResult<TestCommandResult>>> executeFunc,
        Func<TestCommand, CancellationToken, Task<IGenericResult>>? authFunc = null,
        string[]? requiredRoles = null)
    {
        _executeFunc = executeFunc;
        _authFunc = authFunc;
        _requiredRoles = requiredRoles;
    }

    protected override Task<IGenericResult<TestCommandResult>> ExecuteCommand(TestCommand command, CancellationToken ct)
    {
        return _executeFunc(command, ct);
    }

    protected override Task<IGenericResult> CheckCommandAuthorization(TestCommand command, CancellationToken ct)
    {
        return _authFunc?.Invoke(command, ct) ?? base.CheckCommandAuthorization(command, ct);
    }

    protected override string[] GetRequiredRoles()
    {
        return _requiredRoles ?? base.GetRequiredRoles();
    }

    // Public wrappers for testing protected methods
    public string[] PublicGetRequiredRoles() => GetRequiredRoles();
    public Task<IGenericResult> PublicCheckCommandAuthorizationAsync(TestCommand command, CancellationToken ct) => CheckCommandAuthorization(command, ct);
    public Task<IGenericResult<TestCommandResult>> PublicExecuteCommandAsync(TestCommand command, CancellationToken ct) => ExecuteCommand(command, ct);
    public Task<object> PublicExecute(TestCommand command, CancellationToken ct) => Execute(command, ct);
    public Task<IGenericResult> PublicCheckAuthorizationAsync(TestCommand command, CancellationToken ct) => CheckAuthorization(command, ct);
}

// Test implementation for void CommandEndpoint
public class TestVoidCommandEndpoint : CommandEndpoint<TestCommand>
{
    private readonly Func<TestCommand, CancellationToken, Task<IGenericResult>> _executeFunc;

    public TestVoidCommandEndpoint(Func<TestCommand, CancellationToken, Task<IGenericResult>> executeFunc)
    {
        _executeFunc = executeFunc;
    }

    protected override Task<IGenericResult> ExecuteVoidCommand(TestCommand command, CancellationToken ct)
    {
        return _executeFunc(command, ct);
    }

    // Public wrapper for testing protected method
    public Task<IGenericResult> PublicExecuteCommandAsync(TestCommand command, CancellationToken ct) => ExecuteVoidCommand(command, ct);
    public Task<object> PublicExecute(TestCommand command, CancellationToken ct) => Execute(command, ct);
}

public class TestCommand
{
    public string Action { get; set; } = string.Empty;
}

public class TestCommandResult
{
    public bool Success { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class CommandEndpointTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CommandEndpoint_CanBeCreated()
    {
        // Arrange & Act
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult { Success = true })));

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetRequiredRoles_ReturnsDefaultRole()
    {
        // Arrange
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult())));

        // Act
        var roles = endpoint.PublicGetRequiredRoles();

        // Assert
        roles.ShouldNotBeNull();
        roles.ShouldBe(new[] { "User" });
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetRequiredRoles_ReturnsCustomRoles()
    {
        // Arrange
        var customRoles = new[] { "Admin", "Manager" };
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult())),
            null,
            customRoles);

        // Act
        var roles = endpoint.PublicGetRequiredRoles();

        // Assert
        roles.ShouldBe(customRoles);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CheckCommandAuthorizationAsync_ReturnsSuccess_ByDefault()
    {
        // Arrange
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult())));
        var command = new TestCommand();

        // Act
        var result = await endpoint.PublicCheckCommandAuthorizationAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CheckCommandAuthorizationAsync_ReturnsCustomResult()
    {
        // Arrange
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult())),
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("Not authorized"))));
        var command = new TestCommand();

        // Act
        var result = await endpoint.PublicCheckCommandAuthorizationAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("Not authorized");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void VoidCommandEndpoint_CanBeCreated()
    {
        // Arrange & Act
        var endpoint = new TestVoidCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Success()));

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteVoidCommandAsync_Success_ReturnsSuccessResult()
    {
        // Arrange
        var endpoint = new TestVoidCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Success()));
        var command = new TestCommand { Action = "test" };

        // Act
        var result = await endpoint.PublicExecuteCommandAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task Execute_WrapsVoidCommand_Success()
    {
        // Arrange
        var endpoint = new TestVoidCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Success()));
        var command = new TestCommand { Action = "test" };

        // Act
        var result = await endpoint.PublicExecute(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        var genericResult = result as IGenericResult<object>;
        genericResult.ShouldNotBeNull();
        genericResult.IsSuccess.ShouldBeTrue();
        genericResult.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteCommandAsync_WrapsVoidCommand_Failure()
    {
        // Arrange
        var endpoint = new TestVoidCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("Command failed"))));
        var command = new TestCommand { Action = "test" };

        // Act
        var result = await endpoint.PublicExecuteCommandAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("Command failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteCommandAsync_WrapsVoidCommand_FailureWithNullMessage()
    {
        // Arrange
        var endpoint = new TestVoidCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage((string)null!))));
        var command = new TestCommand { Action = "test" };

        // Act
        var result = await endpoint.PublicExecuteCommandAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        // When the source result has null message, the wrapped result uses it or defaults
        (result.CurrentMessage == null || result.CurrentMessage == "Command execution failed").ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CheckAuthorizationAsync_CallsBaseAndCommandAuthorization_WhenBothPass()
    {
        // Arrange
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult())),
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Success()));
        var command = new TestCommand();

        // Act
        var result = await endpoint.PublicCheckAuthorizationAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task CheckAuthorizationAsync_ReturnsFailure_WhenCommandAuthorizationFails()
    {
        // Arrange
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(new TestCommandResult())),
            (cmd, ct) => Task.FromResult<IGenericResult>(GenericResult.Failure(new GenericMessage("Authorization failed"))));
        var command = new TestCommand();

        // Act
        var result = await endpoint.PublicCheckAuthorizationAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("Authorization failed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteCommandAsync_Success_ReturnsResult()
    {
        // Arrange
        var expectedResult = new TestCommandResult { Success = true, Data = "test data" };
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Success(expectedResult)));
        var command = new TestCommand { Action = "test" };

        // Act
        var result = await endpoint.PublicExecuteCommandAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedResult);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExecuteCommandAsync_Failure_ReturnsFailureResult()
    {
        // Arrange
        var endpoint = new TestCommandEndpoint(
            (cmd, ct) => Task.FromResult<IGenericResult<TestCommandResult>>(
                GenericResult<TestCommandResult>.Failure(new GenericMessage("Command execution failed"))));
        var command = new TestCommand { Action = "test" };

        // Act
        var result = await endpoint.PublicExecuteCommandAsync(command, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldBe("Command execution failed");
    }
}
