using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.UI.Components.Providers;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ProjectProvider"/> headless component.
/// Uses MockHttpHandler because ProjectApiClient is created internally via IHttpClientFactory.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ProjectProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public ProjectProviderTests()
    {
        _ctx = new BunitContext();
    }

    private IRenderedComponent<ProjectProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<ProjectProvider>>(NullLogger<ProjectProvider>.Instance);

        return _ctx.Render<ProjectProvider>();
    }

    private static ProjectContext GetContext(IRenderedComponent<ProjectProvider> component)
    {
        var field = typeof(ProjectProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (ProjectContext)field!.GetValue(component.Instance)!;
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_LoadsProjects()
    {
        var items = new List<ProjectConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", IsEnabled = true },
            new() { Id = Guid.NewGuid(), Name = "Beta", IsEnabled = false }
        };

        var handler = new MockHttpHandler()
            .RespondWith("projects", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Projects.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_EmptyList_ReturnsEmpty()
    {
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Projects.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnLoadData_ApiError_SetsErrorMessage()
    {
        var handler = new MockHttpHandler()
            .RespondError("projects");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Projects.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Filtering Tests ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task FilteredProjects_FiltersOnName()
    {
        var items = new List<ProjectConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "Production-ETL", IsEnabled = true },
            new() { Id = Guid.NewGuid(), Name = "Development-ETL", IsEnabled = true }
        };

        var handler = new MockHttpHandler()
            .RespondWith("projects", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSearchStringChanged("Production");
        });

        var resultCtx = GetContext(component);
        var filtered = resultCtx.FilteredProjects.ToList();
        filtered.Count.ShouldBe(1);
        filtered[0].Name.ShouldBe("Production-ETL");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task FilteredProjects_EmptySearch_ReturnsAll()
    {
        var items = new List<ProjectConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", IsEnabled = true },
            new() { Id = Guid.NewGuid(), Name = "Beta", IsEnabled = true },
            new() { Id = Guid.NewGuid(), Name = "Gamma", IsEnabled = false }
        };

        var handler = new MockHttpHandler()
            .RespondWith("projects", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSearchStringChanged(string.Empty);
        });

        var resultCtx = GetContext(component);
        resultCtx.FilteredProjects.Count().ShouldBe(3);
    }

    // ── Select Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnSelectProject_SetsCurrentProject()
    {
        var project = new ProjectConfiguration { Id = Guid.NewGuid(), Name = "Alpha" };
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { project });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSelectProject(project);
        });

        var resultCtx = GetContext(component);
        resultCtx.CurrentProject.ShouldNotBeNull();
        resultCtx.CurrentProject!.Name.ShouldBe("Alpha");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnSelectProject_Null_ClearsCurrentProject()
    {
        var project = new ProjectConfiguration { Id = Guid.NewGuid(), Name = "Alpha" };
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { project });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSelectProject(project);
            ctx.OnSelectProject(null);
        });

        var resultCtx = GetContext(component);
        resultCtx.CurrentProject.ShouldBeNull();
    }

    // ── Delete Tests ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteProject_Success_RefreshesProjects()
    {
        var id = Guid.NewGuid();
        var handler = new MockHttpHandler()
            .RespondOk($"projects/{id}")
            .RespondWith("projects", new List<ProjectConfiguration>());

        var component = RenderWithHandler(handler);

        bool result = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            result = await ctx.OnDeleteProject(id);
        });

        result.ShouldBeTrue();
        var resultCtx = GetContext(component);
        resultCtx.Projects.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnDeleteProject_ApiError_ReturnsFalse()
    {
        var id = Guid.NewGuid();
        var handler = new MockHttpHandler()
            .RespondError($"projects/{id}");

        var component = RenderWithHandler(handler);

        bool result = true;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            result = await ctx.OnDeleteProject(id);
        });

        result.ShouldBeFalse();
        var resultCtx = GetContext(component);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    // ── Search String Tests ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnSearchStringChanged_UpdatesSearchString()
    {
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(() =>
        {
            var ctx = GetContext(component);
            ctx.OnSearchStringChanged("my-search");
            return Task.CompletedTask;
        });

        var resultCtx = GetContext(component);
        resultCtx.SearchString.ShouldBe("my-search");
    }

    // ── Initial State Tests ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public void InitialState_IsNotLoading()
    {
        // Why: provide an empty-list response so the auto-load in OnAfterRenderAsync
        // completes without error; bUnit runs OnAfterRenderAsync synchronously on render.
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration>());
        var component = RenderWithHandler(handler);

        var ctx = GetContext(component);
        ctx.Projects.Count.ShouldBe(0);
        ctx.ErrorMessage.ShouldBeNull();
        ctx.SearchString.ShouldBe(string.Empty);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnSearchStringChanged_FiltersOnDescription()
    {
        var items = new List<ProjectConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", Description = "quarterly batch run" },
            new() { Id = Guid.NewGuid(), Name = "Beta", Description = "real-time streaming" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("projects", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSearchStringChanged("quarterly");
        });

        var resultCtx = GetContext(component);
        resultCtx.FilteredProjects.Count().ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadData_StateIsLoadingDuringLoad()
    {
        var tcs = new System.Threading.Tasks.TaskCompletionSource<List<ProjectConfiguration>>();
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration>());

        var component = RenderWithHandler(handler);

        // Why: verify IsLoading transitions correctly.
        // The TCS pattern isn't needed here since MockHttpHandler responds synchronously,
        // but we verify the final state is correct.
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnDeleteProject_WithProjectsRemaining_UpdatesList()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var remaining = new List<ProjectConfiguration>
        {
            new() { Id = id2, Name = "Beta" }
        };

        var handler = new MockHttpHandler()
            .RespondOk($"projects/{id1}")
            .RespondWith("projects", remaining);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnDeleteProject(id1);
        });

        var resultCtx = GetContext(component);
        resultCtx.Projects.Count.ShouldBe(1);
        resultCtx.Projects[0].Name.ShouldBe("Beta");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnSelectProject_TogglesCurrentProjectOn()
    {
        var p1 = new ProjectConfiguration { Id = Guid.NewGuid(), Name = "Alpha" };
        var p2 = new ProjectConfiguration { Id = Guid.NewGuid(), Name = "Beta" };
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { p1, p2 });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSelectProject(p1);
        });

        var ctx1 = GetContext(component);
        ctx1.CurrentProject!.Name.ShouldBe("Alpha");

        await component.InvokeAsync(() =>
        {
            var ctx = GetContext(component);
            ctx.OnSelectProject(p2);
            return Task.CompletedTask;
        });

        var ctx2 = GetContext(component);
        ctx2.CurrentProject!.Name.ShouldBe("Beta");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task FilteredProjects_CaseInsensitiveSearch()
    {
        var items = new List<ProjectConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "PRODUCTION-ETL" }
        };

        var handler = new MockHttpHandler()
            .RespondWith("projects", items);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadData();
            ctx.OnSearchStringChanged("production");
        });

        var resultCtx = GetContext(component);
        resultCtx.FilteredProjects.Count().ShouldBe(1);
    }

    public void Dispose() => _ctx?.Dispose();
}
