using System.Net.Http;
using Bunit;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RoleDetailPage = Fdw.UI.Pages.Authorization.Pages.RoleDetailPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Auth;

/// <summary>
/// Component tests for the FDW <c>RoleDetail</c> page (Authorization.UI.Pages). Relocated from
/// reference-ui's Auth/RolesPageTests RoleDetail cases. RoleDetail captures its RoleProvider via
/// <c>@ref</c> and casts to the concrete RoleProvider type, which rejects a stub, so these run the
/// REAL RoleProvider against a no-op HTTP factory (empty responses). The page header (RoleName)
/// renders regardless of load state.
/// </summary>
[Trait("Category", "Ui")]
public sealed class RoleDetailPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private IRenderedComponent<RoleDetailPage> Render(string roleName)
    {
        var handler = new MockHttpHandler().RespondWith("roles", Array.Empty<object>());
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") });
        _ctx.Services.AddSingleton(factory.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        return _ctx.Render<RoleDetailPage>(p => p.Add(d => d.RoleName, roleName));
    }

    [Fact]
    public void RendersRoleNameHeaderWithRealProvider()
    {
        var cut = Render("Admin");
        cut.Markup.ShouldContain("Admin");
    }

    [Fact]
    public void DifferentRoleNameFlowsToHeader()
    {
        var cut = Render("Operator");
        cut.Markup.ShouldContain("Operator");
    }

    public void Dispose() => _ctx.Dispose();
}
