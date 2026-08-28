using Fdw.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;

namespace Fdw.Services.Connections.Http.Abstractions.Tests;

[ExcludeFromCodeCoverage]
internal sealed class TestHttpProtocol : HttpProtocolBase
{
    public TestHttpProtocol()
        : base(999, "TestProtocol", "Test protocol for unit tests", "application/json")
    {
    }

    public new HttpMethod GetHttpMethod(IDataCommand command, IStorageContainer container, HttpProtocolContext context)
        => base.GetHttpMethod(command, container, context);

    public new string GetRequestPath(IDataCommand command, IStorageContainer container, HttpProtocolContext context)
        => base.GetRequestPath(command, container, context);

    public new Task<IGenericResult<HttpContent?>> BuildRequestBody(
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
        => base.BuildRequestBody(command, container, context, cancellationToken);

    public new void ConfigureRequestHeaders(
        HttpRequestMessage request,
        IDataCommand command,
        IStorageContainer container,
        HttpProtocolContext context)
        => base.ConfigureRequestHeaders(request, command, container, context);

    public new Task<IGenericResult<object?>> ExtractResult(
        string content,
        HttpResponseMessage response,
        Type resultType,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
        => base.ExtractResult(content, response, resultType, context, cancellationToken);
}

public class HttpProtocolBaseTests
{
    private readonly TestHttpProtocol _protocol;
    private readonly Mock<IDataCommand> _mockCommand;
    private readonly Mock<IStorageContainer> _mockContainer;
    private readonly Mock<IGenericConfiguration> _mockConfiguration;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly HttpProtocolContext _context;

    public HttpProtocolBaseTests()
    {
        _protocol = new TestHttpProtocol();
        _mockCommand = new Mock<IDataCommand>();
        _mockContainer = new Mock<IStorageContainer>();
        _mockConfiguration = new Mock<IGenericConfiguration>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _context = new HttpProtocolContext(
            _mockConfiguration.Object,
            _mockLoggerFactory.Object,
            null,
            null,
            null);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsId()
    {
        _protocol.Id.ShouldBe(999);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsName()
    {
        _protocol.Name.ShouldBe("TestProtocol");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsDescription()
    {
        _protocol.Description.ShouldBe("Test protocol for unit tests");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsDefaultContentType()
    {
        _protocol.DefaultContentType.ShouldBe("application/json");
    }

    [Theory]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    [InlineData("Query", "GET")]
    [InlineData("Insert", "POST")]
    [InlineData("Update", "PUT")]
    [InlineData("Delete", "DELETE")]
    [InlineData("Unknown", "POST")]
    public void GetHttpMethodReturnsCorrectMethodForCommandType(string commandType, string expectedMethod)
    {
        // Arrange
        _mockCommand.Setup(c => c.CommandType).Returns(commandType);

        // Act
        var method = _protocol.GetHttpMethod(_mockCommand.Object, _mockContainer.Object, _context);

        // Assert
        method.Method.ShouldBe(expectedMethod);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetRequestPathUsesContainerPath()
    {
        // Arrange
        var mockPath = new Mock<IPath>();
        mockPath.Setup(p => p.PathValue).Returns("/test/path");
        _mockContainer.Setup(c => c.Path).Returns(mockPath.Object);

        // Act
        var path = _protocol.GetRequestPath(_mockCommand.Object, _mockContainer.Object, _context);

        // Assert
        path.ShouldBe("/test/path");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetRequestPathUsesContainerNameWhenPathIsNull()
    {
        // Arrange
        _mockContainer.Setup(c => c.Path).Returns((IPath)null!);
        _mockContainer.Setup(c => c.Name).Returns("TestContainer");

        // Act
        var path = _protocol.GetRequestPath(_mockCommand.Object, _mockContainer.Object, _context);

        // Assert
        path.ShouldBe("TestContainer");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void GetRequestPathReturnsEmptyWhenBothPathAndContainerNameAreNull()
    {
        // Arrange
        _mockContainer.Setup(c => c.Path).Returns((IPath)null!);
        _mockContainer.Setup(c => c.Name).Returns(string.Empty);

        // Act
        var path = _protocol.GetRequestPath(_mockCommand.Object, _mockContainer.Object, _context);

        // Assert
        path.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildRequestBodyReturnsNullForQueryCommand()
    {
        // Arrange
        _mockCommand.Setup(c => c.CommandType).Returns("Query");

        // Act
        var result = await _protocol.BuildRequestBody(
            _mockCommand.Object,
            _mockContainer.Object,
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildRequestBodyReturnsNullForCommandWithoutInputData()
    {
        // Arrange
        _mockCommand.Setup(c => c.CommandType).Returns("Insert");

        // Act
        var result = await _protocol.BuildRequestBody(
            _mockCommand.Object,
            _mockContainer.Object,
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task BuildRequestBodyReturnsJsonContentForCommandWithInputData()
    {
        // Arrange
        var mockCommandWithInput = new Mock<IDataCommandWithInput>();
        mockCommandWithInput.Setup(c => c.CommandType).Returns("Insert");
        mockCommandWithInput.Setup(c => c.InputData).Returns(new { Name = "Test", Value = 123 });

        // Act
        var result = await _protocol.BuildRequestBody(
            mockCommandWithInput.Object,
            _mockContainer.Object,
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeOfType<StringContent>();

        var content = await result.Value!.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.ShouldContain("name");
        content.ShouldContain("Test");
        content.ShouldContain("value");
        content.ShouldContain("123");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConfigureRequestHeadersSetsAcceptHeader()
    {
        // Arrange
        var request = new HttpRequestMessage();

        // Act
        _protocol.ConfigureRequestHeaders(request, _mockCommand.Object, _mockContainer.Object, _context);

        // Assert
        request.Headers.Accept.ShouldNotBeEmpty();
        request.Headers.Accept.ToString().ShouldBe("application/json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExtractResultDeserializesJsonContent()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        var json = JsonSerializer.Serialize(testObject);
        var response = new HttpResponseMessage();

        // Act
        var result = await _protocol.ExtractResult(
            json,
            response,
            typeof(object),
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ExtractResultReturnsFailureForInvalidJson()
    {
        // Arrange
        const string invalidJson = "{ invalid json";
        var response = new HttpResponseMessage();

        // Act
        var result = await _protocol.ExtractResult(
            invalidJson,
            response,
            typeof(object),
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateReturnsSuccessWithValidCommand()
    {
        // Arrange
        _mockCommand.Setup(c => c.CommandType).Returns("Query");
        _mockContainer.Setup(c => c.Path).Returns((IPath)null!);
        _mockContainer.Setup(c => c.Name).Returns("test");

        // Act
        var result = await _protocol.Translate(
            _mockCommand.Object,
            _mockContainer.Object,
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Method.ShouldBe(HttpMethod.Get);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateIncludesBodyWhenProvided()
    {
        // Arrange
        var mockCommandWithInput = new Mock<IDataCommandWithInput>();
        mockCommandWithInput.Setup(c => c.CommandType).Returns("Insert");
        mockCommandWithInput.Setup(c => c.InputData).Returns(new { Name = "Test" });
        _mockContainer.Setup(c => c.Path).Returns((IPath)null!);
        _mockContainer.Setup(c => c.Name).Returns("test");

        // Act
        var result = await _protocol.Translate(
            mockCommandWithInput.Object,
            _mockContainer.Object,
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Content.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task TranslateConfiguresHeaders()
    {
        // Arrange
        _mockCommand.Setup(c => c.CommandType).Returns("Query");
        _mockContainer.Setup(c => c.Path).Returns((IPath)null!);
        _mockContainer.Setup(c => c.Name).Returns("test");

        // Act
        var result = await _protocol.Translate(
            _mockCommand.Object,
            _mockContainer.Object,
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Headers.Accept.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseReturnsFailureForNonSuccessStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Error content", Encoding.UTF8, "text/plain"),
            ReasonPhrase = "Bad Request"
        };

        // Act
        var result = await _protocol.ProcessResponse(
            response,
            _mockContainer.Object,
            typeof(object),
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseReturnsNullForEmptyContent()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        };

        // Act
        var result = await _protocol.ProcessResponse(
            response,
            _mockContainer.Object,
            typeof(object),
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseReturnsNullForWhitespaceContent()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("   ")
        };

        // Act
        var result = await _protocol.ProcessResponse(
            response,
            _mockContainer.Object,
            typeof(object),
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ProcessResponseExtractsResultForValidContent()
    {
        // Arrange
        var testObject = new { Name = "Test", Value = 123 };
        var json = JsonSerializer.Serialize(testObject);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Act
        var result = await _protocol.ProcessResponse(
            response,
            _mockContainer.Object,
            typeof(object),
            _context,
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }
}
