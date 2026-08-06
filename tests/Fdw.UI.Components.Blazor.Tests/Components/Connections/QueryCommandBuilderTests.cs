using System.Text.Json;
using Bunit;
using Fdw.Services.Connections.UI.Components;

namespace Fdw.UI.Components.Blazor.Tests.Components.Connections;

/// <summary>
/// Component tests for the <see cref="QueryCommandBuilder"/> FDW UI component. Relocated from
/// reference-ui's QueryCommandBuilderTests. The component mutates the supplied TaskConfiguration
/// dictionary in place; these tests assert both the rendered DOM and the dictionary contents
/// after handler invocation, running directly against the component.
/// </summary>
[Trait("Category", "Ui")]
public sealed class QueryCommandBuilderTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    private static Dictionary<string, object?> Config(params (string Key, object? Value)[] entries)
    {
        var d = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (k, v) in entries)
        {
            d[k] = v;
        }

        return d;
    }

    private IRenderedComponent<QueryCommandBuilder> RenderBuilder(
        IDictionary<string, object?> cfg,
        IReadOnlyList<string>? containers = null,
        IReadOnlyList<string>? fields = null) =>
        _ctx.Render<QueryCommandBuilder>(p =>
        {
            p.Add(x => x.TaskConfiguration, cfg);
            if (containers is not null)
            {
                p.Add(x => x.AvailableContainers, containers);
            }

            if (fields is not null)
            {
                p.Add(x => x.AvailableFields, fields);
            }
        });

    [Fact]
    public void ContainerRendersDropdownWhenContainersAvailable()
    {
        var cut = RenderBuilder(Config(), containers: ["dbo.Sales", "dbo.Orders"]);
        cut.FindAll("select").Count.ShouldBeGreaterThanOrEqualTo(1);
        cut.Markup.ShouldContain("dbo.Sales");
        cut.Markup.ShouldContain("Select container...");
    }

    [Fact]
    public void ContainerRendersFreeTextInputWhenNoContainers()
    {
        var cut = RenderBuilder(Config());
        cut.Find("input[placeholder='schema.TableName']").ShouldNotBeNull();
    }

    [Fact]
    public void FieldSectionHiddenUntilContainerChosen()
    {
        var cut = RenderBuilder(Config());
        cut.Markup.ShouldNotContain("Fields");
    }

    [Fact]
    public void ContainerDropdownChangePersistsToConfigAndRevealsFields()
    {
        var cfg = Config();
        var cut = RenderBuilder(cfg, containers: ["dbo.Sales"]);
        cut.Find("select").Change("dbo.Sales");
        cfg["Container"].ShouldBe("dbo.Sales");
        cut.Markup.ShouldContain("Fields");
    }

    [Fact]
    public void ContainerInputInputPersistsToConfig()
    {
        var cfg = Config();
        var cut = RenderBuilder(cfg);
        cut.Find("input[placeholder='schema.TableName']").Input("my.Table");
        cfg["Container"].ShouldBe("my.Table");
    }

    [Fact]
    public void FieldsRendersCheckboxesWhenFieldsAvailable()
    {
        var cut = RenderBuilder(Config(("Container", "dbo.Sales")), fields: ["Id", "Amount"]);
        cut.Markup.ShouldContain("All fields (*)");
        cut.Markup.ShouldContain("Id");
        cut.Markup.ShouldContain("Amount");
    }

    [Fact]
    public void FieldsRendersFreeTextInputWhenNoFieldMetadata()
    {
        var cut = RenderBuilder(Config(("Container", "dbo.Sales")));
        cut.Find("input[placeholder='* (all fields) or comma-separated names']").ShouldNotBeNull();
    }

    [Fact]
    public void AllFieldsCheckboxCheckedByDefaultWhenFieldsEmpty()
    {
        var cut = RenderBuilder(Config(("Container", "dbo.Sales")), fields: ["Id"]);
        var all = cut.FindAll("input[type=checkbox]")[0];
        all.HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void ToggleAllFieldsOffSetsFieldsToEmptyRemovingConfigKey()
    {
        var cfg = Config(("Container", "dbo.Sales"), ("Fields", "Id"));
        var cut = RenderBuilder(cfg, fields: ["Id", "Amount"]);
        cut.FindAll("input[type=checkbox]")[0].Change(false);
        cfg.ContainsKey("Fields").ShouldBeFalse(); // empty => removed
    }

    [Fact]
    public void ToggleFieldFromAllSelectsAllButThatOne()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg, fields: ["Id", "Amount"]);
        // Currently "all" selected => clicking "Id" selects the others (Amount).
        cut.FindAll("input[type=checkbox]")[1].Change(false);
        cfg["Fields"].ShouldBe("Amount");
    }

    [Fact]
    public void FieldsInputFreeTextPersistsToConfig()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg);
        cut.Find("input[placeholder='* (all fields) or comma-separated names']").Input("a,b");
        cfg["Fields"].ShouldBe("a,b");
    }

    [Fact]
    public void FiltersShowEmptyHintWhenNoClauses()
    {
        var cut = RenderBuilder(Config(("Container", "dbo.Sales")));
        cut.Markup.ShouldContain("No filters");
    }

    [Fact]
    public void AddFilterAddsClauseRowAndPersistsJson()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg, fields: ["Id"]);
        cut.FindAll("button").First(b => b.TextContent.Contains("+ Add", StringComparison.Ordinal)).Click();
        cfg.ContainsKey("FilterJson").ShouldBeTrue();
        cut.Markup.ShouldContain("Column...");
    }

    [Fact]
    public void FilterClauseHidesValueInputForIsNullOperator()
    {
        // FilterJson seeded with an IsNull clause => no value input rendered for it.
        var clauses = JsonSerializer.Serialize(new[] { new { Field = "Id", Operator = "IsNull", Value = "" } });
        var cut = RenderBuilder(Config(("Container", "dbo.Sales"), ("FilterJson", clauses)), fields: ["Id"]);
        cut.Markup.ShouldNotContain("placeholder=\"Value\"");
    }

    [Fact]
    public void FilterClauseShowsValueInputForEqualOperator()
    {
        var clauses = JsonSerializer.Serialize(new[] { new { Field = "Id", Operator = "Equal", Value = "5" } });
        var cut = RenderBuilder(Config(("Container", "dbo.Sales"), ("FilterJson", clauses)), fields: ["Id"]);
        cut.FindAll("input").Any(i => string.Equals(i.GetAttribute("placeholder"), "Value", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    public void SecondFilterClauseShowsAndConnector()
    {
        var clauses = JsonSerializer.Serialize(new[]
        {
            new { Field = "Id", Operator = "Equal", Value = "1" },
            new { Field = "Amt", Operator = "Equal", Value = "2" }
        });
        var cut = RenderBuilder(Config(("Container", "dbo.Sales"), ("FilterJson", clauses)));
        cut.Markup.ShouldContain("AND");
    }

    [Fact]
    public void UpdateClauseFieldPersistsToFilterJson()
    {
        var clauses = JsonSerializer.Serialize(new[] { new { Field = "", Operator = "Equal", Value = "" } });
        var cfg = Config(("Container", "dbo.Sales"), ("FilterJson", clauses));
        var cut = RenderBuilder(cfg, fields: ["Id", "Amount"]);
        var clauseFieldSelect = cut.FindAll("select").First(s => s.InnerHtml.Contains("Column...", StringComparison.Ordinal));
        clauseFieldSelect.Change("Amount");
        ((string)cfg["FilterJson"]!).ShouldContain("Amount");
    }

    [Fact]
    public void RemoveFilterRemovesClauseAndClearsJsonWhenEmpty()
    {
        var clauses = JsonSerializer.Serialize(new[] { new { Field = "Id", Operator = "Equal", Value = "1" } });
        var cfg = Config(("Container", "dbo.Sales"), ("FilterJson", clauses));
        var cut = RenderBuilder(cfg, fields: ["Id"]);
        cut.FindAll("button[title='Remove filter']")[0].Click();
        cfg.ContainsKey("FilterJson").ShouldBeFalse();
    }

    [Fact]
    public void SortFieldChangePersistsToConfig()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg, fields: ["Id", "Amount"]);
        var sortSelect = cut.FindAll("select").First(s => s.InnerHtml.Contains("No sort", StringComparison.Ordinal));
        sortSelect.Change("Amount");
        cfg["SortField"].ShouldBe("Amount");
    }

    [Fact]
    public void SortDirectionChangePersistsToConfig()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg, fields: ["Id"]);
        var dirSelect = cut.FindAll("select").First(s => s.InnerHtml.Contains("Descending", StringComparison.Ordinal));
        dirSelect.Change("Desc");
        cfg["SortDirection"].ShouldBe("Desc");
    }

    [Fact]
    public void SkipInputPersistsToConfig()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg);
        cut.Find("input[placeholder='0']").Input("5");
        cfg["Skip"].ShouldBe("5");
    }

    [Fact]
    public void TakeInputPersistsToConfig()
    {
        var cfg = Config(("Container", "dbo.Sales"));
        var cut = RenderBuilder(cfg);
        var takeInput = cut.FindAll("input").Last(i => string.Equals(i.GetAttribute("placeholder"), "0", StringComparison.Ordinal));
        takeInput.Input("100");
        cfg["Take"].ShouldBe("100");
    }

    [Fact]
    public void HydratesLocalStateFromConfigDictOnRender()
    {
        var cut = RenderBuilder(Config(
            ("Container", "dbo.Sales"),
            ("SortField", "Id"),
            ("SortDirection", "Desc"),
            ("Skip", "10")), fields: ["Id"]);
        cut.Find("input[placeholder='0']").GetAttribute("value").ShouldBe("10");
        cut.FindAll("select").First(s => s.InnerHtml.Contains("Descending", StringComparison.Ordinal))
            .GetAttribute("value").ShouldBe("Desc");
    }

    [Fact]
    public void MalformedFilterJsonFallsBackToNoClauses()
    {
        // A bad FilterJson should be swallowed (catch) => empty filter list.
        var cut = RenderBuilder(Config(("Container", "dbo.Sales"), ("FilterJson", "{not valid json")));
        cut.Markup.ShouldContain("No filters");
    }

    public void Dispose() => _ctx.Dispose();
}
