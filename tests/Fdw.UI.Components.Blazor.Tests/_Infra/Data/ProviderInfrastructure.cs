using Bunit;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Fdw.UI.Components.Blazor.Tests.DataInfra;

/// <summary>
/// Registers the minimal services that real FDW providers require via <c>[Inject]</c>
/// (an <see cref="IHttpClientFactory"/> and an <see cref="ILoggerFactory"/>), so that
/// an inheriting provider stub can be constructed by the bUnit renderer without the
/// renderer failing on unresolved injected dependencies.
/// </summary>
/// <remarks>
/// The HTTP client is backed by a no-op handler that returns an empty JSON array; the
/// inheriting stubs override every lifecycle hook, so no request is ever actually sent.
/// </remarks>
public static class ProviderInfrastructure
{
    public static void RegisterProviderInfrastructure(this BunitContext ctx)
    {
        var handler = new MockHttpHandler().WithDefault("[]");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        ctx.Services.AddSingleton(factory.Object);
        ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
    }
}
