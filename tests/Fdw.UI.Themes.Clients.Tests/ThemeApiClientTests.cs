using System.Net;
using System.Net.Http.Json;
using Fdw.UI.Themes.Clients.ApiClients;
using Fdw.UI.Themes.Clients.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.Themes.Clients.Tests;

public sealed class ThemeApiClientTests
{
    private static ThemeApiClient CreateClient(MockHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://test.example.com/") };
        return new ThemeApiClient(httpClient, Mock.Of<ILogger<ThemeApiClient>>());
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task GetThemesSendsCorrectRequest()
    {
        var expected = new List<ThemeSummaryPayload>
        {
            new() { Name = "dark", PrimaryColor = "#000" }
        };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetThemes(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(1);
        result.Value[0].Name.ShouldBe("dark");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task GetThemeSendsCorrectRequest()
    {
        var expected = new ThemeConfiguration { Name = "fractal" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetTheme("fractal", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes/fractal");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("fractal");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task GetDefaultThemeSendsCorrectRequest()
    {
        var expected = new ThemeConfiguration { Name = "default-light", IsDefault = true };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);

        var result = await sut.GetDefaultTheme(TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes/default");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Get);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.IsDefault.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task CreateThemeSendsCorrectRequest()
    {
        var expected = new ThemeConfiguration { Name = "new-theme" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new CreateThemeRequest { Name = "new-theme", DisplayName = "New Theme" };

        var result = await sut.CreateTheme(request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("new-theme");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task UpdateThemeSendsCorrectRequest()
    {
        var expected = new ThemeConfiguration { Name = "my-theme" };
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = CreateClient(handler);
        var request = new UpdateThemeRequest { DisplayName = "Updated Theme" };

        var result = await sut.UpdateTheme("my-theme", request, TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes/my-theme");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Put);
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("my-theme");
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task DeleteThemeSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.DeleteTheme("old-theme", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes/old-theme");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Delete);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task DeleteThemeReturnsFailureOnNotFound()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);

        var result = await sut.DeleteTheme("missing", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task SetDefaultThemeSendsCorrectRequest()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateClient(handler);

        var result = await sut.SetDefaultTheme("fractal", TestContext.Current.CancellationToken);

        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest.RequestUri!.PathAndQuery.ShouldBe("/themes/fractal/default");
        handler.LastRequest.Method.ShouldBe(HttpMethod.Post);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task SetDefaultThemeReturnsFailureOnError()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateClient(handler);

        var result = await sut.SetDefaultTheme("bad", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task GetThemesReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.GetThemes(TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task CreateThemeReturnsFailureOnNonSuccessStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = CreateClient(handler);
        var request = new CreateThemeRequest { Name = "bad" };

        var result = await sut.CreateTheme(request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task UpdateThemeReturnsFailureOnNonSuccessStatusCode()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateClient(handler);
        var request = new UpdateThemeRequest { DisplayName = "Updated" };

        var result = await sut.UpdateTheme("missing", request, TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task DeleteThemeReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.DeleteTheme("err", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Ui")]
    public async Task SetDefaultThemeReturnsFailureOnException()
    {
        var handler = new MockHttpMessageHandler((_, _) => throw new HttpRequestException("Network error"));
        var sut = CreateClient(handler);

        var result = await sut.SetDefaultTheme("err", TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Messages.ShouldNotBeEmpty();
    }
}
