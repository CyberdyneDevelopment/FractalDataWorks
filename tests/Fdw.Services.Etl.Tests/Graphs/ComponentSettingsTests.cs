using Fdw.Services.Etl.Graphs;
using Fdw.Services.Etl.Graphs.Building;
using Microsoft.Extensions.Logging.Abstractions;
using static Fdw.Services.Etl.Tests.Graphs.GraphTestKit;

namespace Fdw.Services.Etl.Tests.Graphs;

/// <summary>
/// Tests for the check of a stored component's settings against the catalogue (FDW-1396): a required setting is stated, a setting
/// the kind does not take is refused, and the message names the component and the setting.
/// </summary>
public sealed class ComponentSettingsTests
{
    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AComponentThatStatesWhatItsKindRequiresPasses()
    {
        ComponentSettings.Check(DataSetSource("src"), NullLogger.Instance).IsSuccess.ShouldBeTrue();
        ComponentSettings.Check(Mapped("map", ("A", "B")), NullLogger.Instance).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ARequiredSettingLeftOutIsRefusedNamingTheComponentAndTheSetting()
    {
        var component = DataSetSource("src");
        component.BatchSize = null;

        var result = ComponentSettings.Check(component, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("src");
        result.CurrentMessage.ShouldContain("batchSize");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void ASettingTheKindDoesNotTakeIsRefused()
    {
        var component = DataSetSource("src");
        component.JoinKind = "Inner";

        var result = ComponentSettings.Check(component, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("joinKind");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AKindThatIsNotInTheCatalogueIsRefused()
    {
        var component = DataSetSource("src");
        component.ComponentType = "Teleport";

        var result = ComponentSettings.Check(component, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("Teleport");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataIntegrity")]
    public void AListRowOfASettingTheKindDoesNotTakeIsRefused()
    {
        var component = Mapped("map", ("A", "B"));
        component.Columns.Add(new PipelineComponentColumnConfiguration { SettingName = "leftKeys", Ordinal = 0, ColumnName = "Id" });

        var result = ComponentSettings.Check(component, NullLogger.Instance);

        result.IsFailure.ShouldBeTrue();
        result.CurrentMessage.ShouldNotBeNull().ShouldContain("leftKeys");
    }
}
