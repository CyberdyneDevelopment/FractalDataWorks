using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Clients;
using Fdw.UI.Components.Blazor.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Fdw.UI.Components.Blazor.Tests.Providers;

/// <summary>
/// Tests for <see cref="ProjectApiClient"/> covering HTTP endpoint mappings and result handling.
/// Uses MockHttpHandler to control HTTP responses without spinning up a real server.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ProjectApiClientTests
{
    private static ProjectApiClient CreateClient(MockHttpHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            NullLogger<ProjectApiClient>.Instance);

    // ── ListProjects ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task ListProjects_Success_ReturnsProjects()
    {
        var items = new List<ProjectConfiguration>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha" },
            new() { Id = Guid.NewGuid(), Name = "Beta" }
        };

        var handler = new MockHttpHandler().RespondWith("projects", items);
        var client = CreateClient(handler);

        var result = await client.ListProjects(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Count.ShouldBe(2);
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ListProjects_ApiError_ReturnsFailure()
    {
        var handler = new MockHttpHandler().RespondError("projects");
        var client = CreateClient(handler);

        var result = await client.ListProjects(TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── GetProject ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task GetProject_Success_ReturnsProject()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectConfiguration { Id = projectId, Name = "Alpha" };

        var handler = new MockHttpHandler().RespondWith($"projects/{projectId}", project);
        var client = CreateClient(handler);

        var result = await client.GetProject(projectId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("Alpha");
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task GetProject_NotFound_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        var handler = new MockHttpHandler().RespondError($"projects/{projectId}");
        var client = CreateClient(handler);

        var result = await client.GetProject(projectId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── DeleteProject ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task DeleteProject_Success_ReturnsSuccess()
    {
        var projectId = Guid.NewGuid();
        var handler = new MockHttpHandler().RespondOk($"projects/{projectId}");
        var client = CreateClient(handler);

        var result = await client.DeleteProject(projectId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    public async Task DeleteProject_ApiError_ReturnsFailure()
    {
        var projectId = Guid.NewGuid();
        var handler = new MockHttpHandler().RespondError($"projects/{projectId}");
        var client = CreateClient(handler);

        var result = await client.DeleteProject(projectId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── CancellationToken ────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P2")]
    public async Task ListProjects_CancelledToken_ReturnsFailure()
    {
        var handler = new MockHttpHandler();
        var client = CreateClient(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await client.ListProjects(cts.Token);

        result.IsSuccess.ShouldBeFalse();
    }

    // ── GetExecutionStatus ───────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    public async Task GetExecutionStatus_ApiError_ReturnsFailure()
    {
        var executionId = Guid.NewGuid();
        var handler = new MockHttpHandler().RespondError($"executions/{executionId}");
        var client = CreateClient(handler);

        var result = await client.GetExecutionStatus(executionId, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
    }
}
