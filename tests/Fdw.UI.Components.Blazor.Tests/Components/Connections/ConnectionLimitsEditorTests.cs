using Bunit;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.UI.Components;
using Microsoft.AspNetCore.Components;

namespace Fdw.UI.Components.Blazor.Tests.Components.Connections;

/// <summary>
/// Component tests for the <see cref="ConnectionLimitsEditor"/> FDW UI component. Relocated from
/// reference-ui's ConnectionLimitsEditorTests, which asserted the editor's render branches and
/// add/remove callbacks through the hosted reference-ui form; here they run directly against the
/// component with plain collection parameters.
/// </summary>
[Trait("Category", "Ui")]
public sealed class ConnectionLimitsEditorTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    /// <summary>A limit type whose DisplayName/Description drive the label branches.</summary>
    private sealed class FakeLimitType : ConnectionLimitTypeBase
    {
        public FakeLimitType(string name, string display, string desc)
            : base(1, name, display, desc, [])
        {
        }
    }

    /// <summary>A limit config with the base ctor wired but all enforcement values null.</summary>
    private sealed class PlainLimitConfig : ConnectionLimitConfiguration
    {
        public PlainLimitConfig() : base("Connection", "MsSql", "Connections:Limits") { }
    }

    /// <summary>A limit config that exposes enforcement values to exercise the value-row branches.</summary>
    private sealed class FullLimitConfig : ConnectionLimitConfiguration
    {
        public FullLimitConfig() : base("Connection", "MsSql", "Connections:Limits") { }

        public override int? EnforceMaxPerSecond => 10;
        public override int? EnforceBurstSize => 20;
        public override int? EnforceMaxConcurrent => 5;
        public override int? EnforceTimeoutSeconds => 30;
        public override int? EnforceMaxRows => 1000;
        public override int? EnforceMaxQueriesPerDay => 50000;
        public override long? EnforceMaxBytesPerDay => 1048576L;
    }

    private IRenderedComponent<ConnectionLimitsEditor> RenderEditor(
        IReadOnlyList<ConnectionLimitConfiguration> limits,
        IReadOnlyList<IConnectionLimitType> types,
        Action<ComponentParameterCollectionBuilder<ConnectionLimitsEditor>>? extra = null) =>
        _ctx.Render<ConnectionLimitsEditor>(p =>
        {
            p.Add(x => x.Limits, limits);
            p.Add(x => x.LimitTypes, types);
            extra?.Invoke(p);
        });

    [Fact]
    public void RendersEmptyStateWhenNoLimits()
    {
        var cut = RenderEditor([], [new FakeLimitType("RateLimit", "Rate Limit", "desc")]);
        cut.Markup.ShouldContain("No limits configured");
    }

    [Fact]
    public void RendersTypeOptionsWithDisplayName()
    {
        var cut = RenderEditor([], [new FakeLimitType("RateLimit", "Rate Limit", "desc")]);
        cut.FindAll("option").Any(o => o.TextContent.Contains("Rate Limit", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void RendersLimitCardWithDisplayLabelAndDescription()
    {
        var cfg = new FullLimitConfig { LimitType = "RateLimit", Name = "L1" };
        var cut = RenderEditor([cfg], [new FakeLimitType("RateLimit", "Rate Limit", "Token bucket")]);
        cut.Markup.ShouldContain("Rate Limit");
        cut.Markup.ShouldContain("Token bucket");
    }

    [Fact]
    public void RendersLimitCardFallsBackToLimitTypeWhenTypeUnknown()
    {
        var cfg = new FullLimitConfig { LimitType = "Mystery", Name = "L1" };
        var cut = RenderEditor([cfg], []);
        // No matching IConnectionLimitType => displayLabel falls back to limit.LimitType.
        cut.Markup.ShouldContain("Mystery");
    }

    [Fact]
    public void RendersAllEnforcementValueRowsWhenPresent()
    {
        var cfg = new FullLimitConfig { LimitType = "RateLimit", Name = "L1" };
        var cut = RenderEditor([cfg], [new FakeLimitType("RateLimit", "Rate Limit", "d")]);
        cut.Markup.ShouldContain("Max Per Second");
        cut.Markup.ShouldContain("Burst Size");
        cut.Markup.ShouldContain("Max Concurrent");
        cut.Markup.ShouldContain("Timeout (s)");
        cut.Markup.ShouldContain("Max Rows");
        cut.Markup.ShouldContain("Max Queries/Day");
        cut.Markup.ShouldContain("Max Bytes/Day");
        cut.Markup.ShouldContain("1048576");
    }

    [Fact]
    public void RendersNoEnforcementRowsWhenBaseConfigHasNullValues()
    {
        // Plain config: every Enforce* returns null => no value rows.
        var cfg = new PlainLimitConfig { LimitType = "RateLimit", Name = "L1" };
        var cut = RenderEditor([cfg], [new FakeLimitType("RateLimit", "Rate Limit", "d")]);
        cut.Markup.ShouldNotContain("Max Per Second");
        cut.Markup.ShouldContain("Rate Limit");
    }

    [Fact]
    public void AddButtonDisabledWhenNoTypeSelected()
    {
        var cut = RenderEditor([], [new FakeLimitType("RateLimit", "Rate Limit", "d")]);
        cut.FindAll("button").First(b => b.TextContent.Contains("Add Limit", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task SelectingTypeEnablesAddAndAddInvokesCallback()
    {
        string? added = null;
        var cut = RenderEditor([], [new FakeLimitType("RateLimit", "Rate Limit", "d")],
            p => p.Add(x => x.OnAddLimit, EventCallback.Factory.Create<string>(this, n => added = n)));

        cut.Find("select").Change("RateLimit");
        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Add Limit", StringComparison.Ordinal));
        addBtn.HasAttribute("disabled").ShouldBeFalse();
        addBtn.Click();
        await Task.Yield();

        added.ShouldBe("RateLimit");
    }

    [Fact]
    public async Task RemoveButtonInvokesOnRemoveWithLimit()
    {
        var cfg = new FullLimitConfig { LimitType = "RateLimit", Name = "L1" };
        ConnectionLimitConfiguration? removed = null;
        var cut = RenderEditor([cfg], [new FakeLimitType("RateLimit", "Rate Limit", "d")],
            p => p.Add(x => x.OnRemoveLimit, EventCallback.Factory.Create<ConnectionLimitConfiguration>(this, c => removed = c)));

        cut.FindAll("button[title='Remove limit']")[0].Click();
        await Task.Yield();

        removed.ShouldBe(cfg);
    }

    public void Dispose() => _ctx.Dispose();
}
