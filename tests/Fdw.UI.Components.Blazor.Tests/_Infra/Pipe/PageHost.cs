using System.Net.Http;
using Bunit;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.PipeInfra;

/// <summary>
/// Registers the minimal services FDW page components and their headless providers require via
/// <c>[Inject]</c> (an <see cref="IHttpClientFactory"/> backed by a no-op JSON handler and an
/// <see cref="ILoggerFactory"/>). When a page's provider is swapped for a stub, no HTTP request is
/// ever sent — these registrations only satisfy the renderer's constructor-injection needs.
/// </summary>
public static class PageHost
{
    public static void RegisterPageInfrastructure(this BunitContext ctx)
    {
        var handler = new MockHttpHandler().WithDefault(System.Net.HttpStatusCode.OK);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://localhost/api/") });

        ctx.Services.AddSingleton(factory.Object);
        ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
    }
}
