using System.Net.Http;
using System.Security.Claims;
using Bunit;
using Fdw.Services.Notifications.Clients.Models;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationSettingsPage = Fdw.Services.Messaging.UI.Pages.Pages.NotificationSettings;

namespace Fdw.UI.Components.Blazor.Tests.Components.Messaging;

/// <summary>
/// Component tests for the FDW <c>NotificationSettings</c> page (Messaging.UI.Pages). Relocated from
/// reference-ui's Messaging/NotificationSettingsPageTests. Unlike the provider-stubbed pages this
/// one drives a real NotificationApiClient over HttpClient, so a MockHttpHandler feeds the
/// preference list. Asserts preference rows, toggle checkboxes, Save/Reset, and that Save/Reset hit
/// the API.
/// </summary>
[Trait("Category", "Ui")]
public sealed class NotificationSettingsPageContentTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private MockHttpHandler ConfigureHttp()
    {
        var handler = new MockHttpHandler()
            .RespondWith("notification-preferences",
                new[]
                {
                    new UserNotificationPreferenceResponse { NotificationType = "Alert", Channel = "Email", IsEnabled = true },
                    new UserNotificationPreferenceResponse { NotificationType = "Digest", Channel = "InApp", IsEnabled = false },
                });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/api/") });
        _ctx.Services.AddSingleton(factory.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        // Authenticated user with a parseable NameIdentifier so _userId is set.
        var authContext = _ctx.AddAuthorization();
        authContext.SetAuthorized("tester");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        return handler;
    }

    [Fact]
    public void RendersPreferenceRowsAfterLoad()
    {
        ConfigureHttp();
        var cut = _ctx.Render<NotificationSettingsPage>();
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Alert");
            cut.Markup.ShouldContain("Digest");
        });
    }

    [Fact]
    public void RendersToggleSwitchesReflectingEnabledState()
    {
        ConfigureHttp();
        var cut = _ctx.Render<NotificationSettingsPage>();
        // Why: the page renders each preference as a clickable ".sw" toggle span (".sw on" when
        // enabled), not an <input type=checkbox> — assert the real toggle markup.
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("span.sw").Count.ShouldBe(2);
            cut.FindAll("span.sw.on").Count.ShouldBe(1); // only "Alert" starts enabled
        });
    }

    [Fact]
    public void RendersSaveAndResetButtons()
    {
        ConfigureHttp();
        var cut = _ctx.Render<NotificationSettingsPage>();
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button").Any(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).ShouldBeTrue();
            cut.FindAll("button").Any(b => b.TextContent.Contains("Reset", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }

    [Fact]
    public void ToggleSwitchFlipsState()
    {
        ConfigureHttp();
        var cut = _ctx.Render<NotificationSettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("span.sw").Count.ShouldBe(2));
        // Digest starts disabled (".sw" without ".on"); clicking it flips to ".sw on".
        cut.FindAll("span.sw")[1].Click();
        cut.WaitForAssertion(() => cut.FindAll("span.sw.on").Count.ShouldBe(2));
    }

    [Fact]
    public void SaveInvokesApiWithoutError()
    {
        var handler = ConfigureHttp();
        var cut = _ctx.Render<NotificationSettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).ShouldBeTrue());
        cut.FindAll("button").First(b => b.TextContent.Contains("Save", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => handler.Requests.Any(r => r.Method == HttpMethod.Put).ShouldBeTrue());
    }

    [Fact]
    public void ResetReloadsPreferences()
    {
        var handler = ConfigureHttp();
        var cut = _ctx.Render<NotificationSettingsPage>();
        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Reset", StringComparison.Ordinal)).ShouldBeTrue());
        var before = handler.Requests.Count;
        cut.FindAll("button").First(b => b.TextContent.Contains("Reset", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => handler.Requests.Count.ShouldBeGreaterThan(before));
    }

    public void Dispose() => _ctx.Dispose();
}
