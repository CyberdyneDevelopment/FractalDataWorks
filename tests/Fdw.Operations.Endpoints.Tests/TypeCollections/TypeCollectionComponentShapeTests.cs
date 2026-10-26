using System.Linq;
using System.Text.Json;
using Fdw.Operations.Endpoints.TypeCollections;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Graphs.Tools;
using Shouldly;
using Xunit;

namespace Fdw.Operations.Endpoints.Tests.TypeCollections;

/// <summary>
/// Tests that the type-collection value summary lists a component kind's nodes and settings as data the console draws from
/// (FDW-1395), and that the catalogue the console reads is consistent with itself.
/// </summary>
[Trait("Priority", "P1")]
[Trait("Category", "Operations")]
public class TypeCollectionComponentShapeTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AMergeJoinListsItsTwoFixedInputsAndOneOutput()
    {
        var summary = TypeCollectionResolver.ToSummary(new MergeJoinComponentType());

        summary.Inputs.ShouldNotBeNull().Select(n => n.Name).ShouldBe(["left", "right"]);
        summary.Outputs.ShouldNotBeNull().Select(n => n.Name).ShouldBe(["output"]);
        summary.Inputs.ShouldAllBe(n => !n.IsNamedByUser && n.Min == 1 && n.Max == 1);
    }

    [Fact]
    public void ASourceListsNoInputsAndADestinationNoOutputs()
    {
        TypeCollectionResolver.ToSummary(new DataSetSourceComponentType()).Inputs.ShouldNotBeNull().ShouldBeEmpty();
        TypeCollectionResolver.ToSummary(new DataSetDestinationComponentType()).Outputs.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public void AnOptionThatIsNotAComponentListsNoNodes()
    {
        var summary = TypeCollectionResolver.ToSummary(new InsertLoadStepKind());

        summary.Inputs.ShouldBeNull();
        summary.Outputs.ShouldBeNull();
    }

    [Fact]
    public void ASettingIsListedAsAParameterWithItsKindAndRequirement()
    {
        var summary = TypeCollectionResolver.ToSummary(new MergeJoinComponentType());

        var kind = summary.Parameters.Single(p => p.Name == "joinKind");
        kind.Kind.ShouldBe("JoinKinds");
        kind.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void TheNodesAreListedByTheirNamesInTheJson()
    {
        var json = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new LookupComponentType()), Web);

        json.ShouldContain("\"inputs\":[");
        json.ShouldContain("\"name\":\"reference\"");
        json.ShouldContain("\"isError\":false");
    }

    [Fact]
    public void EveryComponentKindHasUniqueNodeNamesSettingNamesAndSaneCounts()
    {
        foreach (var kind in ComponentTypes.All())
        {
            kind.Inputs.Select(n => n.Name).Distinct().Count().ShouldBe(kind.Inputs.Count, kind.Name);
            kind.Outputs.Select(n => n.Name).Distinct().Count().ShouldBe(kind.Outputs.Count, kind.Name);
            kind.Settings.Select(s => s.Name).Distinct().Count().ShouldBe(kind.Settings.Count, kind.Name);
            kind.Inputs.Concat(kind.Outputs).ShouldAllBe(n => n.Min >= 0 && (n.Max == null || n.Max >= n.Min), kind.Name);
            kind.Inputs.ShouldAllBe(n => !n.IsError, kind.Name);
        }
    }

    [Fact]
    public void EverySettingKindIsOneTheConsoleRendersOrACollectionThatExists()
    {
        string[] rendered =
        [
            "Text", "Number", "Boolean", "Expression", "ColumnList", "FieldMaps", "InputMappings", "SplitOutputs", "ValueSources", "DataSet", "DataStore",
            "Container", "Pipeline", "Variable", "Parameters", "Steps", "SqlText",
            // The members of the stage-backed tools (Filter, Calculate, Sort, Aggregate, Distinct, Latest by, Validate).
            "SortFieldList", "Calculation", "Aggregation", "Distinct", "LatestBy", "CheckList", "ValidateFailAction", "ComputeSpec", "FieldMappings",
        ];
        string[] collections = ["LoadStepKinds", "JoinKinds", "SplitRoutings", "SoftDeleteModes", "RunPoints"];

        var unknown = ComponentTypes.All().SelectMany(k => k.Settings).Select(s => s.Kind)
            .Where(kind => !rendered.Contains(kind) && !collections.Contains(kind)).Distinct().ToList();

        unknown.ShouldBeEmpty();
        JoinKinds.All().Select(k => k.Name).ShouldBe(["FullOuter", "Inner", "LeftOuter"], ignoreOrder: true);
        SplitRoutings.All().Select(k => k.Name).ShouldBe(["AllMatches", "FirstMatch"], ignoreOrder: true);
    }

    [Fact]
    public void AnActionKindListsItsSettingsAndWhatItHoldsInTheJson()
    {
        var json = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new ForLoopComponentType()), Web);

        json.ShouldContain("\"name\":\"variable\"");
        json.ShouldContain("\"name\":\"end\"");
        json.ShouldContain("\"holdsComponents\":true");
        json.ShouldContain("\"isAction\":true");
    }

    [Fact]
    public void EveryActionKindHasUniqueSettingNamesAndAKindTheConsoleRenders()
    {
        string[] rendered = ["Text", "Number", "Boolean", "Variable", "Parameters", "SqlText", "RunPoints"];

        foreach (var kind in ComponentTypes.All().Where(candidate => candidate.IsAction))
        {
            kind.Settings.Select(s => s.Name).Distinct().Count().ShouldBe(kind.Settings.Count, kind.Name);
            kind.Settings.ShouldAllBe(s => rendered.Contains(s.Kind), kind.Name);
        }
    }

    [Fact]
    public void ASettingThatShapesTheGraphIsListedAsNotBindable()
    {
        var stage = TypeCollectionResolver.ToSummary(new SetVariableComponentType()).Parameters.Single(p => p.Name == "stage");

        stage.Bindable.ShouldBeFalse();
        stage.AcceptsParameterText.ShouldBeFalse();
    }

    [Fact]
    public void ASingleValueSettingIsListedAsBindableAndOnlyTheNamesOfASourceTakeParameterText()
    {
        var source = TypeCollectionResolver.ToSummary(new DataSetSourceComponentType()).Parameters;

        var dataSet = source.Single(p => p.Name == "dataSet");
        dataSet.Bindable.ShouldBeTrue();
        dataSet.AcceptsParameterText.ShouldBeTrue();
        var batch = source.Single(p => p.Name == "batchSize");
        batch.Bindable.ShouldBeTrue();
        batch.AcceptsParameterText.ShouldBeFalse();
    }

    [Fact]
    public void AListOfColumnsAndTheMappingsAreListedAsBindableToAListParameterAndOtherListsAreNot()
    {
        var destination = TypeCollectionResolver.ToSummary(new ContainerDestinationComponentType()).Parameters;
        var columns = destination.Single(p => p.Name == "writeColumns");
        var mappings = destination.Single(p => p.Name == "mappings");

        columns.Bindable.ShouldBeTrue();
        columns.BindsToList.ShouldBeTrue();
        destination.Single(p => p.Name == "loadKind").BindsToList.ShouldBeFalse();
        mappings.Bindable.ShouldBeTrue();
        mappings.BindsToList.ShouldBeTrue();
    }

    [Fact]
    public void AStepSettingThatNamesAVariableIsListedAsBindable()
    {
        var variable = TypeCollectionResolver.ToSummary(new SetVariableComponentType()).Parameters.Single(p => p.Name == "variable");

        variable.Bindable.ShouldBeTrue();
    }

    [Fact]
    public void TheFlagsAreListedByTheirNamesInTheJson()
    {
        var json = JsonSerializer.Serialize(TypeCollectionResolver.ToSummary(new DataSetSourceComponentType()), Web);

        json.ShouldContain("\"bindable\":true");
        json.ShouldContain("\"acceptsParameterText\":true");
    }

    [Fact]
    public void ABindableSettingListsTheParameterTypeTheSaveAccepts()
    {
        var source = TypeCollectionResolver.ToSummary(new DataSetSourceComponentType()).Parameters;
        var destination = TypeCollectionResolver.ToSummary(new DataSetDestinationComponentType()).Parameters;

        source.Single(p => p.Name == "dataSet").BindsTo.ShouldBe("String");
        source.Single(p => p.Name == "batchSize").BindsTo.ShouldBe("Int32");
        destination.Single(p => p.Name == "truncateFirst").BindsTo.ShouldBe("Bool");
        destination.Single(p => p.Name == "loadKind").BindsTo.ShouldBe("String");
        destination.Single(p => p.Name == "keyColumns").BindsTo.ShouldBe("String");
    }

    [Fact]
    public void TheKeyColumnsOfADistinctToolAreListedAsBindableToAListParameter()
    {
        var keys = TypeCollectionResolver.ToSummary(new DistinctComponentType()).Parameters.Single(p => p.Name == "distinct");

        keys.Bindable.ShouldBeTrue();
        keys.BindsToList.ShouldBeTrue();
        keys.BindsTo.ShouldBe("String");
    }

    [Fact]
    public void TheFieldsOfASortToolAreListedAsBindableToAListParameter()
    {
        var fields = TypeCollectionResolver.ToSummary(new SortComponentType()).Parameters.Single(p => p.Name == "sort");

        fields.Bindable.ShouldBeTrue();
        fields.BindsToList.ShouldBeTrue();
        fields.BindsTo.ShouldBe("String");
    }

    [Fact]
    public void ASettingARunCannotSupplyListsNoParameterType()
    {
        TypeCollectionResolver.ToSummary(new SetVariableComponentType()).Parameters.Single(p => p.Name == "stage").BindsTo.ShouldBeNull();
    }

    [Fact]
    public void AnActionSettingListsTheParameterTypeTheSaveAccepts()
    {
        var parameters = TypeCollectionResolver.ToSummary(new ForLoopComponentType()).Parameters;

        parameters.Single(p => p.Name == "variable").BindsTo.ShouldBe("String");
        parameters.Single(p => p.Name == "start").BindsTo.ShouldBe("Int32");
        TypeCollectionResolver.ToSummary(new ExecutePipelineComponentType()).Parameters.Single(p => p.Name == "awaitCompletion").BindsTo.ShouldBe("Bool");
    }

    [Fact]
    public void EveryBindableSettingOfTheCatalogueListsAParameterTypeAndNoOtherDoes()
    {
        var all = ComponentTypes.All().Select(TypeCollectionResolver.ToSummary).SelectMany(kind => kind.Parameters).ToList();

        all.Where(p => p.Bindable).ShouldAllBe(p => p.BindsTo == "String" || p.BindsTo == "Int32" || p.BindsTo == "Bool");
        all.Where(p => !p.Bindable).ShouldAllBe(p => p.BindsTo == null);
    }
}
