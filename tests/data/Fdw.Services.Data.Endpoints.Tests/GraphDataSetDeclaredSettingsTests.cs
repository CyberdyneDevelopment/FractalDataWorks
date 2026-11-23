using Fdw.Services.Etl.Graphs;
using Shouldly;
using Xunit;

namespace Fdw.Services.Data.Endpoints.Tests;

/// <summary>
/// FDW-6133: a Graph data set keeps the settings a component type declares that no column holds. The component is copied from the graph pipeline's
/// reader into <c>data.GraphDataSetComponentSetting</c> rows, text that names variables as {{Name}} included, and none is named as a setting the data set cannot hold.
/// </summary>
public sealed class GraphDataSetDeclaredSettingsTests
{
    private static PipelineComponentConfiguration Component() => new()
    {
        Name = "ask",
        ComponentType = "AskModel",
        DeclaredSettings =
        [
            new PipelineComponentSettingConfiguration { SettingName = "model", ValueText = "large" },
            new PipelineComponentSettingConfiguration { SettingName = "question", ValueText = "{{Question}}" },
        ],
    };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AGraphDataSetComponentKeepsEachDeclaredSettingWithItsText()
    {
        var source = Component();

        var copied = GraphDataSetComponents.Read(source);

        copied.DeclaredSettings.Count.ShouldBe(2);
        copied.DeclaredSettings[0].Id.ShouldBe(source.DeclaredSettings[0].Id);
        copied.DeclaredSettings[0].SettingName.ShouldBe("model");
        copied.DeclaredSettings[0].ValueText.ShouldBe("large");
        copied.DeclaredSettings[1].ValueText.ShouldBe("{{Question}}");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ADeclaredSettingIsNotASettingTheGraphDataSetRefuses()
    {
        GraphDataSetComponents.UnsupportedSettings(Component()).ShouldBeEmpty();
    }
}
