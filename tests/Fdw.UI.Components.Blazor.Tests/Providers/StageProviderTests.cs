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
/// Tests for <see cref="StageProvider"/> headless component.
/// Uses MockHttpHandler because StageProvider creates ProjectApiClient internally via IHttpClientFactory.
/// Stage CRUD is exercised through the project update path (GetProject + mutate Stages + UpdateProject).
/// </summary>
[Trait("Category", "Ui")]
public sealed class StageProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public StageProviderTests()
    {
        _ctx = new BunitContext();
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<StageProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<StageProvider>>(NullLogger<StageProvider>.Instance);

        return _ctx.Render<StageProvider>();
    }

    private static StageContext GetContext(IRenderedComponent<StageProvider> component)
    {
        var field = typeof(StageProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (StageContext)field!.GetValue(component.Instance)!;
    }

    private static ProjectConfiguration MakeProject(Guid projectId, params StageConfiguration[] stages)
    {
        var project = new ProjectConfiguration { Id = projectId, Name = "Test Project", IsEnabled = true };
        foreach (var s in stages)
        {
            project.Stages.Add(s);
        }

        return project;
    }

    // ── Initial State Tests ──────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public void InitialState_IsNotLoading()
    {
        var handler = new MockHttpHandler();
        var component = RenderWithHandler(handler);
        var ctx = GetContext(component);

        ctx.IsLoading.ShouldBeFalse();
        ctx.Stages.Count.ShouldBe(0);
        ctx.CurrentStage.ShouldBeNull();
        ctx.ErrorMessage.ShouldBeNull();
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadStages_LoadsStagesFromProject()
    {
        var projectId = Guid.NewGuid();
        var stage1 = new StageConfiguration { Id = Guid.NewGuid(), Name = "Stage A", Ordinal = 1, ProjectConfigurationId = projectId };
        var stage2 = new StageConfiguration { Id = Guid.NewGuid(), Name = "Stage B", Ordinal = 2, ProjectConfigurationId = projectId };
        var project = MakeProject(projectId, stage1, stage2);

        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", project);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Stages.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
        resultCtx.ProjectId.ShouldBe(projectId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadStages_EmptyProject_ReturnsEmpty()
    {
        var projectId = Guid.NewGuid();
        var project = MakeProject(projectId);

        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", project);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
        });

        var resultCtx = GetContext(component);
        resultCtx.Stages.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnLoadStages_ApiError_SetsErrorMessage()
    {
        var projectId = Guid.NewGuid();
        var handler = new MockHttpHandler()
            .RespondError($"projects/{projectId}");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Stages.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnLoadStages_OrdersByOrdinal()
    {
        var projectId = Guid.NewGuid();
        var stage1 = new StageConfiguration { Id = Guid.NewGuid(), Name = "Second", Ordinal = 2, ProjectConfigurationId = projectId };
        var stage2 = new StageConfiguration { Id = Guid.NewGuid(), Name = "First", Ordinal = 1, ProjectConfigurationId = projectId };
        var project = MakeProject(projectId, stage1, stage2);

        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", project);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
        });

        var resultCtx = GetContext(component);
        resultCtx.Stages[0].Name.ShouldBe("First");
        resultCtx.Stages[1].Name.ShouldBe("Second");
    }

    // ── Select Tests ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnSelectStage_SetsCurrentStage()
    {
        var projectId = Guid.NewGuid();
        var stage = new StageConfiguration { Id = Guid.NewGuid(), Name = "Stage A", Ordinal = 1, ProjectConfigurationId = projectId };
        var project = MakeProject(projectId, stage);

        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", project);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
            ctx.OnSelectStage(stage);
        });

        var resultCtx = GetContext(component);
        resultCtx.CurrentStage.ShouldNotBeNull();
        resultCtx.CurrentStage!.Name.ShouldBe("Stage A");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnSelectStage_Null_ClearsCurrentStage()
    {
        var projectId = Guid.NewGuid();
        var stage = new StageConfiguration { Id = Guid.NewGuid(), Name = "Stage A", Ordinal = 1, ProjectConfigurationId = projectId };
        var project = MakeProject(projectId, stage);

        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", project);

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
            ctx.OnSelectStage(stage);
            ctx.OnSelectStage(null);
        });

        var resultCtx = GetContext(component);
        resultCtx.CurrentStage.ShouldBeNull();
    }

    // ── Delete Tests ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnDeleteStage_Success_RefreshesStages()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var stage = new StageConfiguration { Id = stageId, Name = "Stage A", Ordinal = 1, ProjectConfigurationId = projectId };
        var projectWithStage = MakeProject(projectId, stage);
        // After delete, project has no stages.
        var projectEmpty = MakeProject(projectId);

        // Why: First load uses the project with a stage; after delete (update), refresh sees empty project.
        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", projectWithStage)
            .RespondOk($"projects/{projectId}");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
        });

        // Reconfigure handler so post-delete refresh returns empty project.
        handler.RespondWith($"projects/{projectId}", projectEmpty);

        bool deleted = false;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteStage(stageId);
        });

        deleted.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnDeleteStage_NoProjectLoaded_ReturnsFalse()
    {
        // Why: If LoadStages was never called, _projectId is null — delete must fail cleanly.
        var stageId = Guid.NewGuid();
        var handler = new MockHttpHandler();

        var component = RenderWithHandler(handler);

        bool deleted = true;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteStage(stageId);
        });

        deleted.ShouldBeFalse();
        var resultCtx = GetContext(component);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnDeleteStage_ApiError_ReturnsFalse()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var stage = new StageConfiguration { Id = stageId, Name = "Stage A", Ordinal = 1, ProjectConfigurationId = projectId };
        var project = MakeProject(projectId, stage);

        var handler = new MockHttpHandler()
            .RespondWith($"projects/{projectId}", project)
            .RespondError($"projects");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadStages(projectId);
        });

        // Reconfigure so update (PUT) returns error.
        handler.RespondError($"projects/{projectId}");

        bool deleted = true;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteStage(stageId);
        });

        deleted.ShouldBeFalse();
    }
}
