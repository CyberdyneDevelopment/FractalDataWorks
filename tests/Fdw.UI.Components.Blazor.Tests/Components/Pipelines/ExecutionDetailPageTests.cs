using Bunit;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Operations.Clients.Models;
using Fdw.Operations.Components.Execution;
using Fdw.UI.Components.Blazor.Tests.PipeInfra;
using ExecutionDetailPage = Fdw.UI.Pages.Pipelines.Pages.Pipelines.ExecutionDetailPage;

namespace Fdw.UI.Components.Blazor.Tests.Components.Pipelines;

/// <summary>
/// Component tests for the pipeline ExecutionDetail page (ExecutionDetail.razor). Relocated from
/// reference-ui's ExecutionDetailTests, which asserted these behaviours through the hosted page
/// driven by HTTP; here the page is rendered directly with a SEEDED <see cref="ExecutionDetailContext"/>
/// (its provider swapped for a stub), asserting the same rendered markup at the FDW layer.
/// </summary>
[Trait("Category", "Ui")]
[Collection(PageHostCollection.Name)]
public sealed class ExecutionDetailPageTests : IDisposable
{
    private readonly BunitContext _ctx = new();
    private readonly Guid _execId = Guid.NewGuid();

    private static ExecutionSummaryPayload Exec(string name, string state, string type = "Pipeline",
        string? correlationId = null, DateTimeOffset? completedAt = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            ItemType = type,
            State = state,
            CorrelationId = correlationId,
            CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = completedAt,
        };

    private IRenderedComponent<ExecutionDetailPage> RenderWith(ExecutionDetailContext context)
    {
        _ctx.RegisterPageInfrastructure();
        _ctx.ComponentFactories.Add(new ProviderFactory<ExecutionDetailProvider, ExecutionDetailContext>(context));
        return _ctx.Render<ExecutionDetailPage>(p => p.Add(x => x.ExecutionId, _execId));
    }

    [Fact]
    public void RendersErrorBannerAndNotFoundWhenExecutionNull()
    {
        var cut = RenderWith(new ExecutionDetailContext { LastResult = GenericResult.Failure(new GenericMessage("boom")), Execution = null });
        cut.Markup.ShouldContain("boom");
        cut.Markup.ShouldContain("Execution not found");
    }

    [Fact]
    public void RendersLoadingStateWhenLoadingAndNoExecution()
    {
        var cut = RenderWith(new ExecutionDetailContext { IsLoading = true, Execution = null });
        cut.Markup.ShouldContain("Loading execution");
    }

    [Fact]
    public void RendersExecutionSummaryCardWithoutOptionalBlocks()
    {
        var cut = RenderWith(new ExecutionDetailContext { Execution = Exec("nightly-run", "Running") });
        cut.Markup.ShouldContain("nightly-run");
        cut.Markup.ShouldContain("Running");
        cut.Markup.ShouldContain("No child executions found.");
        cut.Markup.ShouldNotContain("Correlation ID");
        cut.Markup.ShouldNotContain(">Completed<");
    }

    [Fact]
    public void RendersCorrelationIdAndCompletedAtWhenPresent()
    {
        var cut = RenderWith(new ExecutionDetailContext
        {
            Execution = Exec("done-run", "Completed", correlationId: "corr-123", completedAt: DateTimeOffset.UtcNow),
        });
        cut.Markup.ShouldContain("Correlation ID");
        cut.Markup.ShouldContain("corr-123");
        cut.Markup.ShouldContain("Completed");
    }

    [Fact]
    public void RendersStepsTableWhenChildrenPresent()
    {
        var cut = RenderWith(new ExecutionDetailContext
        {
            Execution = Exec("parent", "Running"),
            Children = [Exec("step-1", "Completed"), Exec("step-2", "Failed")],
        });
        cut.Markup.ShouldContain("Steps (2)");
        cut.Markup.ShouldContain("step-1");
        cut.Markup.ShouldContain("step-2");
    }

    [Fact]
    public void StateBadgeMapsFailedToFailClass()
    {
        var cut = RenderWith(new ExecutionDetailContext { Execution = Exec("p", "Failed") });
        cut.FindAll("span").ShouldContain(s => s.ClassList.Contains("b-fail"));
    }

    [Fact]
    public void RefreshButtonInvokesOnRefreshCallback()
    {
        var refreshed = false;
        var cut = RenderWith(new ExecutionDetailContext
        {
            Execution = Exec("p", "Running"),
            OnRefresh = () => { refreshed = true; return Task.FromResult<IGenericResult>(GenericResult.Success()); },
        });
        cut.FindAll("button").First(b => b.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();
        refreshed.ShouldBeTrue();
    }

    public void Dispose() => _ctx.Dispose();
}
