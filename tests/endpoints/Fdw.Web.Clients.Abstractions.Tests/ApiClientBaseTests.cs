using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Clients.Abstractions.Tests;

public sealed class ApiClientBaseTests
{
    private static readonly Uri BaseUri = new("https://test.example.com/");

    private static TestApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseUri };
        return new TestApiClient(httpClient, Mock.Of<ILogger<ApiClientBase>>());
    }

    private static MockHttpMessageHandler OkHandler(TestModel model) =>
        new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(model)
        });

    private static MockHttpMessageHandler OkHandlerNoBody() =>
        new(new HttpResponseMessage(HttpStatusCode.OK));

    private static MockHttpMessageHandler NullBodyHandler() =>
        new(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("null", Encoding.UTF8, "application/json")
        });

    private static MockHttpMessageHandler ErrorHandler(HttpStatusCode status) =>
        new(new HttpResponseMessage(status));

    private static MockHttpMessageHandler ThrowingHandler() =>
        new((_, _) => throw new HttpRequestException("Connection refused"));

    private static MockHttpMessageHandler JsonExceptionHandler() =>
        new((_, _) => throw new JsonException("Invalid JSON"));

    private static MockHttpMessageHandler UnexpectedExceptionHandler() =>
        new((_, _) => throw new InvalidOperationException("Unexpected error"));

    // --- Constructor ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ConstructorSetsHttpClientAndLogger()
    {
        var httpClient = new HttpClient() { BaseAddress = BaseUri };
        var logger = Mock.Of<ILogger<ApiClientBase>>();
        var sut = new TestApiClient(httpClient, logger);

        sut.ShouldNotBeNull();
    }

    // --- Get<T> ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetReturnsSuccessWithDeserializedValueOnOkResponse()
    {
        var expected = new TestModel { Name = "test", Value = 42 };
        var handler = OkHandler(expected);
        var sut = CreateClient(handler);

        var result = await sut.Get<TestModel>("api/items", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("test");
        result.Value.Value.ShouldBe(42);
        handler.LastRequest.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetReturnsFailureWhenDeserializationReturnsNull()
    {
        var handler = NullBodyHandler();
        var sut = CreateClient(handler);

        var result = await sut.Get<TestModel>("api/items", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Get<TestModel>("api/items", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Get<TestModel>("api/items", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task GetReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Get<TestModel>("api/items", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Post<TRequest, TResponse> ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithBodyReturnsSuccessWithDeserializedResponse()
    {
        var expected = new TestModel { Name = "created", Value = 1 };
        var handler = OkHandler(expected);
        var sut = CreateClient(handler);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel { Name = "new" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("created");
        result.Value.Value.ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithBodyReturnsFailureWhenDeserializationReturnsNull()
    {
        var handler = NullBodyHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithBodyReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.BadRequest);
        var sut = CreateClient(handler);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithBodyReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithBodyReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithBodyReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post<TestModel, TestModel>("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Post<TRequest> (no response body) ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithRequestReturnsSuccessOnOkStatus()
    {
        var handler = OkHandlerNoBody();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items", new TestModel { Name = "test" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithRequestReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.BadRequest);
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithRequestReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithRequestReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithRequestReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Post (no body, no response) ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostNoBodyReturnsSuccessOnOkStatus()
    {
        var handler = OkHandlerNoBody();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items/action", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostNoBodyReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.InternalServerError);
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostNoBodyReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostNoBodyReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Post("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- PostWithResponse<TResponse> (no request body) ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithResponseReturnsSuccessWithDeserializedValue()
    {
        var expected = new TestModel { Name = "result", Value = 99 };
        var handler = OkHandler(expected);
        var sut = CreateClient(handler);

        var result = await sut.PostWithResponse<TestModel>("api/items/action", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("result");
        result.Value.Value.ShouldBe(99);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithResponseReturnsFailureWhenDeserializationReturnsNull()
    {
        var handler = NullBodyHandler();
        var sut = CreateClient(handler);

        var result = await sut.PostWithResponse<TestModel>("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithResponseReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.NotFound);
        var sut = CreateClient(handler);

        var result = await sut.PostWithResponse<TestModel>("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithResponseReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.PostWithResponse<TestModel>("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithResponseReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.PostWithResponse<TestModel>("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PostWithResponseReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.PostWithResponse<TestModel>("api/items/action", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Put<TRequest, TResponse> ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithBodyReturnsSuccessWithDeserializedResponse()
    {
        var expected = new TestModel { Name = "updated", Value = 2 };
        var handler = OkHandler(expected);
        var sut = CreateClient(handler);

        var result = await sut.Put<TestModel, TestModel>("api/items/1", new TestModel { Name = "update" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("updated");
        result.Value.Value.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithBodyReturnsFailureWhenDeserializationReturnsNull()
    {
        var handler = NullBodyHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithBodyReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.BadRequest);
        var sut = CreateClient(handler);

        var result = await sut.Put<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithBodyReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithBodyReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithBodyReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Put<TRequest> (no response body) ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithRequestReturnsSuccessOnOkStatus()
    {
        var handler = OkHandlerNoBody();
        var sut = CreateClient(handler);

        var result = await sut.Put("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithRequestReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.Conflict);
        var sut = CreateClient(handler);

        var result = await sut.Put("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithRequestReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithRequestReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PutWithRequestReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Put("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Patch<TRequest, TResponse> ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PatchReturnsSuccessWithDeserializedResponse()
    {
        var expected = new TestModel { Name = "patched", Value = 3 };
        var handler = OkHandler(expected);
        var sut = CreateClient(handler);

        var result = await sut.Patch<TestModel, TestModel>("api/items/1", new TestModel { Name = "patch" }, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("patched");
        result.Value.Value.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PatchReturnsFailureWhenDeserializationReturnsNull()
    {
        var handler = NullBodyHandler();
        var sut = CreateClient(handler);

        var result = await sut.Patch<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PatchReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.UnprocessableEntity);
        var sut = CreateClient(handler);

        var result = await sut.Patch<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PatchReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Patch<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PatchReturnsFailureOnJsonException()
    {
        var handler = JsonExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Patch<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task PatchReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Patch<TestModel, TestModel>("api/items/1", new TestModel(), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    // --- Delete ---

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DeleteReturnsSuccessOnOkStatus()
    {
        var handler = OkHandlerNoBody();
        var sut = CreateClient(handler);

        var result = await sut.Delete("api/items/1", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DeleteReturnsFailureOnNonSuccessStatus()
    {
        var handler = ErrorHandler(HttpStatusCode.NotFound);
        var sut = CreateClient(handler);

        var result = await sut.Delete("api/items/1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DeleteReturnsFailureOnHttpRequestException()
    {
        var handler = ThrowingHandler();
        var sut = CreateClient(handler);

        var result = await sut.Delete("api/items/1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task DeleteReturnsFailureOnUnexpectedException()
    {
        var handler = UnexpectedExceptionHandler();
        var sut = CreateClient(handler);

        var result = await sut.Delete("api/items/1", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
