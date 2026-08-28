using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Aui.Models;
using Fdw.UI.WebMcp;
using Fdw.UI.WebMcp.Tests.Fakes;
using Shouldly;
using Xunit;

namespace Fdw.UI.WebMcp.Tests;

/// <summary>
/// Tests for <see cref="AuiToolExtensions"/> — projecting an existing AUI declaration onto the
/// WebMCP UI layer without re-describing the tool.
/// </summary>
public sealed class AuiToolExtensionsTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();

    private static AuiTool SampleTool() => new()
    {
        Name = "run_pipeline",
        Description = "Run a pipeline by name.",
        InputSchema = """{"type":"object","properties":{"name":{"type":"string"}}}""",
        RequiresConfirmation = true,
    };

    private static async Task<string> Invoke(CapturingAuiAction action, string argumentsJson)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        return await SampleTool()
            .ToWebMcpTool(action, UserId)
            .Execute(document.RootElement, CancellationToken.None);
    }

    // ── Metadata mapping ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public void CarriesTheAuiDeclarationOntoTheWebMcpTool()
    {
        var mapped = SampleTool().ToWebMcpTool(new CapturingAuiAction(), UserId);

        mapped.Name.ShouldBe("run_pipeline");
        mapped.Description.ShouldBe("Run a pipeline by name.");
        mapped.InputSchema.ShouldContain("\"type\":\"object\"");
        mapped.RequiresConfirmation.ShouldBeTrue();
    }

    // ── Argument flattening ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task FlattensPrimitiveArgumentsToTheirClrEquivalents()
    {
        var action = new CapturingAuiAction();

        await Invoke(action, """{"name":"nightly","retries":3,"force":true,"ratio":1.5}""");

        var captured = action.Captured.ShouldNotBeNull();
        captured["name"].ShouldBe("nightly");
        captured["retries"].ShouldBe(3L);
        captured["force"].ShouldBe(true);
        captured["ratio"].ShouldBe(1.5d);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public async Task OmitsJsonNullArgumentsRatherThanPassingASentinel()
    {
        var action = new CapturingAuiAction();

        await Invoke(action, """{"name":"nightly","note":null}""");

        action.Captured.ShouldNotBeNull().ContainsKey("note").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public async Task PassesNestedStructuresThroughAsRawJson()
    {
        var action = new CapturingAuiAction();

        await Invoke(action, """{"options":{"deep":1},"tags":["a","b"]}""");

        var captured = action.Captured.ShouldNotBeNull();
        captured["options"].ToString().ShouldNotBeNull().ShouldContain("deep");
        captured["tags"].ToString().ShouldNotBeNull().ShouldContain("a");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Ui")]
    public async Task RunsTheActionAsTheSuppliedUser()
    {
        var action = new CapturingAuiAction();

        await Invoke(action, "{}");

        action.CapturedUserId.ShouldBe(UserId);
    }

    // ── Result mapping ────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Ui")]
    public async Task ReportsSuccessBackToTheAgent()
    {
        (await Invoke(new CapturingAuiAction(), "{}")).ShouldContain("\"success\":true");
    }
}
