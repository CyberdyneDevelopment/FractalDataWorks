using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using Fdw.Messages;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.Base;

namespace Fdw.Web.RestEndpoints.Tests.Base;

// Test implementation of GenericEndpoint for testing
public class TestGenericEndpoint : GenericEndpoint<TestRequest, TestResponse>
{
    private readonly Func<TestRequest, CancellationToken, Task<object>> _executeFunc;

    public TestGenericEndpoint(Func<TestRequest, CancellationToken, Task<object>> executeFunc)
    {
        _executeFunc = executeFunc;
    }

    public override Task<object> Execute(TestRequest request, CancellationToken ct)
    {
        return _executeFunc(request, ct);
    }

    public new ILogger Logger => base.Logger;
    public new IDataGateway DataGateway => base.DataGateway;
    public new string[] CreateErrorMessages(IGenericResult result) => base.CreateErrorMessages(result);
}

public class TestRequest
{
    public string Name { get; set; } = string.Empty;
}

public class TestResponse
{
    public string Message { get; set; } = string.Empty;
}

public class GenericEndpointTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RecEndpoint_CanBeCreated()
    {
        // Arrange & Act
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));

        // Assert
        endpoint.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateErrorMessages_WithCurrentMessage_ReturnsMessage()
    {
        // Arrange
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));
        var result = GenericResult.Failure(new GenericMessage("Test error message"));

        // Act
        var messages = endpoint.CreateErrorMessages(result);

        // Assert
        messages.ShouldNotBeNull();
        messages.Length.ShouldBe(1);
        messages[0].ShouldBe("Test error message");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateErrorMessages_WithoutCurrentMessage_ReturnsDefaultMessage()
    {
        // Arrange
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));
        var result = GenericResult.Failure(new GenericMessage(string.Empty));

        // Act
        var messages = endpoint.CreateErrorMessages(result);

        // Assert
        messages.ShouldNotBeNull();
        messages.Length.ShouldBe(1);
        messages[0].ShouldBe("An error occurred");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateErrorMessages_WithNullMessage_ReturnsDefaultMessage()
    {
        // Arrange
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));
        var result = GenericResult.Failure(new GenericMessage((string)null!));

        // Act
        var messages = endpoint.CreateErrorMessages(result);

        // Assert
        messages.ShouldNotBeNull();
        messages.Length.ShouldBe(1);
        messages[0].ShouldBe("An error occurred");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateErrorMessages_WithEmptyMessage_ReturnsDefaultMessage()
    {
        // Arrange
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));
        var result = GenericResult.Failure(new GenericMessage(""));

        // Act
        var messages = endpoint.CreateErrorMessages(result);

        // Assert
        messages.ShouldNotBeNull();
        messages.Length.ShouldBe(1);
        messages[0].ShouldBe("An error occurred");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateErrorMessages_WithWhitespaceMessage_ReturnsMessage()
    {
        // Arrange
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));
        var result = GenericResult.Failure(new GenericMessage("  "));

        // Act
        var messages = endpoint.CreateErrorMessages(result);

        // Assert
        messages.ShouldNotBeNull();
        messages.Length.ShouldBe(1);
        messages[0].ShouldBe("  ");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateErrorMessages_WithMultilineMessage_ReturnsFullMessage()
    {
        // Arrange
        var endpoint = new TestGenericEndpoint((req, ct) => Task.FromResult<object>(new TestResponse()));
        var result = GenericResult.Failure(new GenericMessage("Error line 1\nError line 2"));

        // Act
        var messages = endpoint.CreateErrorMessages(result);

        // Assert
        messages.ShouldNotBeNull();
        messages.Length.ShouldBe(1);
        messages[0].ShouldBe("Error line 1\nError line 2");
    }
}
