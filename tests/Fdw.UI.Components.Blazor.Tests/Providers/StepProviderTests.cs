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
/// Tests for <see cref="StepProvider"/> headless component.
/// Uses MockHttpHandler because StepProvider creates ProjectApiClient internally via IHttpClientFactory.
/// Step CRUD traverses Project → Stage → Steps (no dedicated Step API).
/// </summary>
[Trait("Category", "Ui")]
public sealed class StepProviderTests : IDisposable
{
    private readonly BunitContext _ctx;

    public StepProviderTests()
    {
        _ctx = new BunitContext();
    }

    public void Dispose() => _ctx.Dispose();

    private IRenderedComponent<StepProvider> RenderWithHandler(MockHttpHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        _ctx.Services.AddSingleton(factoryMock.Object);
        _ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _ctx.Services.AddSingleton<ILogger<StepProvider>>(NullLogger<StepProvider>.Instance);

        return _ctx.Render<StepProvider>();
    }

    private static StepContext GetContext(IRenderedComponent<StepProvider> component)
    {
        var field = typeof(StepProvider).GetField("_context",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (StepContext)field!.GetValue(component.Instance)!;
    }

    private static ProjectConfiguration MakeProject(Guid projectId, Guid stageId, params StepConfiguration[] steps)
    {
        var stage = new StageConfiguration
        {
            Id = stageId,
            Name = "Stage A",
            Ordinal = 1,
            ProjectConfigurationId = projectId
        };
        foreach (var s in steps)
        {
            stage.Steps.Add(s);
        }

        var project = new ProjectConfiguration { Id = projectId, Name = "Test Project", IsEnabled = true };
        project.Stages.Add(stage);
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
        ctx.Steps.Count.ShouldBe(0);
        ctx.CurrentStep.ShouldBeNull();
        ctx.ErrorMessage.ShouldBeNull();
    }

    // ── Load Tests ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnLoadSteps_LoadsStepsFromStage_ViaListProjects()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var step1 = new StepConfiguration { Id = Guid.NewGuid(), Name = "Step 1", Ordinal = 1, ProjectStageConfigurationId = stageId };
        var step2 = new StepConfiguration { Id = Guid.NewGuid(), Name = "Step 2", Ordinal = 2, ProjectStageConfigurationId = stageId };
        var project = MakeProject(projectId, stageId, step1, step2);

        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { project });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSteps(stageId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Steps.Count.ShouldBe(2);
        resultCtx.ErrorMessage.ShouldBeNull();
        resultCtx.StageId.ShouldBe(stageId);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnLoadSteps_StageNotFound_SetsErrorMessage()
    {
        var stageId = Guid.NewGuid();
        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration>());

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSteps(stageId);
        });

        var resultCtx = GetContext(component);
        resultCtx.Steps.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnLoadSteps_ApiError_SetsErrorMessage()
    {
        var stageId = Guid.NewGuid();
        var handler = new MockHttpHandler()
            .RespondError("projects");

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSteps(stageId);
        });

        var resultCtx = GetContext(component);
        resultCtx.IsLoading.ShouldBeFalse();
        resultCtx.Steps.Count.ShouldBe(0);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnLoadSteps_OrdersByOrdinal()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var step1 = new StepConfiguration { Id = Guid.NewGuid(), Name = "Second", Ordinal = 2, ProjectStageConfigurationId = stageId };
        var step2 = new StepConfiguration { Id = Guid.NewGuid(), Name = "First", Ordinal = 1, ProjectStageConfigurationId = stageId };
        var project = MakeProject(projectId, stageId, step1, step2);

        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { project });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSteps(stageId);
        });

        var resultCtx = GetContext(component);
        resultCtx.Steps[0].Name.ShouldBe("First");
        resultCtx.Steps[1].Name.ShouldBe("Second");
    }

    // ── Select Tests ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task OnSelectStep_SetsCurrentStep()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var step = new StepConfiguration { Id = Guid.NewGuid(), Name = "Step A", Ordinal = 1, ProjectStageConfigurationId = stageId };
        var project = MakeProject(projectId, stageId, step);

        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { project });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSteps(stageId);
            ctx.OnSelectStep(step);
        });

        var resultCtx = GetContext(component);
        resultCtx.CurrentStep.ShouldNotBeNull();
        resultCtx.CurrentStep!.Name.ShouldBe("Step A");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnSelectStep_Null_ClearsCurrentStep()
    {
        var projectId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var step = new StepConfiguration { Id = Guid.NewGuid(), Name = "Step A", Ordinal = 1, ProjectStageConfigurationId = stageId };
        var project = MakeProject(projectId, stageId, step);

        var handler = new MockHttpHandler()
            .RespondWith("projects", new List<ProjectConfiguration> { project });

        var component = RenderWithHandler(handler);

        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            await ctx.OnLoadSteps(stageId);
            ctx.OnSelectStep(step);
            ctx.OnSelectStep(null);
        });

        var resultCtx = GetContext(component);
        resultCtx.CurrentStep.ShouldBeNull();
    }

    // ── Delete Tests ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public async Task OnDeleteStep_NoProjectLoaded_ReturnsFalse()
    {
        var stepId = Guid.NewGuid();
        var handler = new MockHttpHandler();

        var component = RenderWithHandler(handler);

        bool deleted = true;
        await component.InvokeAsync(async () =>
        {
            var ctx = GetContext(component);
            deleted = await ctx.OnDeleteStep(stepId);
        });

        deleted.ShouldBeFalse();
        var resultCtx = GetContext(component);
        resultCtx.ErrorMessage.ShouldNotBeNullOrEmpty();
    }
}
