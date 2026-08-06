using System;
using System.Collections.Generic;
using System.Net.Http;
using Fdw.Commands.Data.Abstractions;
using Shouldly;
using Xunit;

namespace Fdw.Commands.Data.Abstractions.Tests.Commands;

/// <summary>
/// Tests for HttpConnectionCommand.
/// </summary>
public sealed class HttpConnectionCommandTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        // Arrange
        var method = HttpMethod.Get;
        var path = "/api/test";

        // Act
        var command = new HttpConnectionCommand(method, path);

        // Assert
        command.Method.ShouldBe(method);
        command.RelativePath.ShouldBe(path);
        command.QueryParameters.ShouldNotBeNull();
        command.QueryParameters.ShouldBeEmpty();
        command.Body.ShouldBeNull();
        command.Headers.ShouldNotBeNull();
        command.Headers.ShouldBeEmpty();
        command.CommandId.ShouldNotBe(Guid.Empty);
        command.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        command.CommandType.ShouldBe("HttpConnection");
        command.Category.ShouldBe("Connection");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithQueryParametersInitializesCorrectly()
    {
        // Arrange
        var method = HttpMethod.Get;
        var path = "/api/test";
        var queryParams = new Dictionary<string, string>
        {
            ["filter"] = "status eq 'active'",
            ["top"] = "10"
        };

        // Act
        var command = new HttpConnectionCommand(method, path, queryParams);

        // Assert
        command.QueryParameters.ShouldBe(queryParams);
        command.QueryParameters.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithBodyInitializesCorrectly()
    {
        // Arrange
        var method = HttpMethod.Post;
        var path = "/api/test";
        var body = "{\"name\":\"test\"}";

        // Act
        var command = new HttpConnectionCommand(method, path, body: body);

        // Assert
        command.Body.ShouldBe(body);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithHeadersInitializesCorrectly()
    {
        // Arrange
        var method = HttpMethod.Get;
        var path = "/api/test";
        var headers = new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token123",
            ["Accept"] = "application/json"
        };

        // Act
        var command = new HttpConnectionCommand(method, path, headers: headers);

        // Assert
        command.Headers.ShouldBe(headers);
        command.Headers.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorWithAllParametersInitializesCorrectly()
    {
        // Arrange
        var method = HttpMethod.Put;
        var path = "/api/test";
        var queryParams = new Dictionary<string, string> { ["id"] = "123" };
        var body = "{\"name\":\"updated\"}";
        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        // Act
        var command = new HttpConnectionCommand(method, path, queryParams, body, headers);

        // Assert
        command.Method.ShouldBe(method);
        command.RelativePath.ShouldBe(path);
        command.QueryParameters.ShouldBe(queryParams);
        command.Body.ShouldBe(body);
        command.Headers.ShouldBe(headers);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenMethodIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new HttpConnectionCommand(null!, "/api/test"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenRelativePathIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new HttpConnectionCommand(HttpMethod.Get, null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void DifferentHttpMethodsAreSupported()
    {
        // Assert GET
        var getCommand = new HttpConnectionCommand(HttpMethod.Get, "/api/test");
        getCommand.Method.ShouldBe(HttpMethod.Get);

        // Assert POST
        var postCommand = new HttpConnectionCommand(HttpMethod.Post, "/api/test");
        postCommand.Method.ShouldBe(HttpMethod.Post);

        // Assert PUT
        var putCommand = new HttpConnectionCommand(HttpMethod.Put, "/api/test");
        putCommand.Method.ShouldBe(HttpMethod.Put);

        // Assert PATCH
        var patchCommand = new HttpConnectionCommand(HttpMethod.Patch, "/api/test");
        patchCommand.Method.ShouldBe(HttpMethod.Patch);

        // Assert DELETE
        var deleteCommand = new HttpConnectionCommand(HttpMethod.Delete, "/api/test");
        deleteCommand.Method.ShouldBe(HttpMethod.Delete);
    }
}
