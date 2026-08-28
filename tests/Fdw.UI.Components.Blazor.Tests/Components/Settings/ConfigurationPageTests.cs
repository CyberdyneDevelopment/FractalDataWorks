using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Configuration.Components.Configuration;
using Fdw.Configuration.UI.Components;
using Fdw.Operations.Clients.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using ConfigurationPage = Fdw.UI.Pages.Configuration.Pages.ConfigurationPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Settings;

/// <summary>
/// Component tests for the FDW Configuration page (<c>Pages/Configuration.razor</c>). Relocated from
/// reference-ui's ConfigurationPageTests. The page consumes a <see cref="ConfigurationPageProvider"/>
/// whose context is the record <see cref="ConfigurationPageContext"/> (no parameterless ctor), so a
/// dedicated stub composes the record from a seeded <see cref="ConfigurationContext"/>. Covers the
/// loading branch, the page-local error channel (context ErrorMessage is intentionally unwired), the
/// category sidebar, the OnLoadInstances callback on category select, instance listing, and the
/// empty-types message.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ConfigurationPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private void Swap(ConfigurationContext inner) =>
        _ctx.ComponentFactories.Add(new ConfigPageFactory(inner));

    private static ConfigurationTypeSummary Type(string category) => new()
    {
        TypeName = $"{category}Config",
        DisplayName = category,
        Category = category,
    };

    private static ConfigurationInstanceSummaryPayload Instance(string name, string category = "Connections") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        ServiceType = "MsSql",
        Category = category,
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public void RendersPageLandmark()
    {
        Swap(new ConfigurationContext { Types = [Type("Connections")] });
        var cut = _ctx.Render<ConfigurationPage>();
        cut.FindAll(".pagehead").Count.ShouldBeGreaterThan(0);
        cut.Markup.ShouldContain("Configuration");
    }

    [Fact]
    public void LoadingBranchWhenTypesLoadingAndEmpty()
    {
        Swap(new ConfigurationContext { IsLoading = true });
        var cut = _ctx.Render<ConfigurationPage>();
        cut.Markup.ShouldNotContain("No configuration types found");
        cut.Markup.ShouldContain("Loading");
    }

    [Fact]
    public void ContextErrorIsNotSurfacedAtPageLevel()
    {
        // The page's error banner binds to its own private _errorMessage (set during create/delete
        // actions), NOT the provider context's ErrorMessage — so a context error renders no banner.
        Swap(new ConfigurationContext { LastResult = GenericResult.Failure(new GenericMessage("config blew up")) });
        var cut = _ctx.Render<ConfigurationPage>();
        cut.Markup.ShouldNotContain("config blew up");
    }

    [Fact]
    public void TypesLoadedRendersCategorySidebar()
    {
        Swap(new ConfigurationContext { Types = [Type("Connections"), Type("Pipelines")] });
        var cut = _ctx.Render<ConfigurationPage>();
        cut.Markup.ShouldContain("Connections");
        cut.Markup.ShouldContain("Pipelines");
        cut.Markup.ShouldNotContain("No configuration types found");
    }

    [Fact]
    public async Task SelectCategoryInvokesOnLoadInstances()
    {
        string? loaded = null;
        Swap(new ConfigurationContext
        {
            Types = [Type("Connections")],
            OnLoadInstances = cat => { loaded = cat; return Task.CompletedTask; },
        });
        var cut = _ctx.Render<ConfigurationPage>();
        cut.FindAll("button").First(b => b.TextContent.Contains("Connections", StringComparison.Ordinal)).Click();
        await Task.Yield();
        loaded.ShouldBe("Connections");
    }

    [Fact]
    public void InstancesLoadedRendersInstanceNames()
    {
        Swap(new ConfigurationContext
        {
            Types = [Type("Connections")],
            Instances = [Instance("PlatformConfiguration"), Instance("AuthDb")],
        });
        var cut = _ctx.Render<ConfigurationPage>();
        // Select the category so the instance pane shows.
        cut.FindAll("button").First(b => b.TextContent.Contains("Connections", StringComparison.Ordinal)).Click();
        cut.Markup.ShouldContain("PlatformConfiguration");
        cut.Markup.ShouldContain("AuthDb");
    }

    [Fact]
    public void EmptyTypesShowsNoTypesMessage()
    {
        Swap(new ConfigurationContext
        {
            Types = [],
            Instances = [Instance("PlatformConfiguration")],
        });
        var cut = _ctx.Render<ConfigurationPage>();
        cut.Markup.ShouldContain("No configuration types found");
    }

    public void Dispose() => _ctx.Dispose();

    /// <summary>
    /// bUnit factory that swaps the real <see cref="ConfigurationPageProvider"/> for a stub which
    /// composes the <see cref="ConfigurationPageContext"/> record from a seeded
    /// <see cref="ConfigurationContext"/>.
    /// </summary>
    private sealed class ConfigPageFactory : IComponentFactory
    {
        private readonly ConfigurationContext _inner;

        public ConfigPageFactory(ConfigurationContext inner) => _inner = inner;

        public bool CanCreate(Type componentType) => componentType == typeof(ConfigurationPageProvider);

        public IComponent Create(Type componentType) => new ConfigPageProviderStub(_inner);
    }

    private sealed class ConfigPageProviderStub : ComponentBase
    {
        private readonly ConfigurationContext _inner;

        public ConfigPageProviderStub(ConfigurationContext inner) => _inner = inner;

        [Parameter] public RenderFragment<ConfigurationPageContext>? ChildContent { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            if (ChildContent is null)
            {
                return;
            }

            builder.AddContent(0, ChildContent(new ConfigurationPageContext(
                _inner,
                [],
                (_, _) => Task.CompletedTask)));
        }
    }
}
